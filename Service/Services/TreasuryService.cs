using Domain.Entities;
using Domain.Loans;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class TreasuryService : ITreasuryService
    {
        private const int MaxPageSize = 100;
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        private readonly ITreasuryRepository _treasury;
        private readonly IBaseRepository<OwnBank> _bank;
        private readonly IBankDashboardService _dashboard;

        // Ayları avtomatik bağlama bu aydan başlayır (köhnə aylar üçün geriyə dönük xərc yazılmır). Əl ilə istənilən tamamlanmış ay bağlana bilər
        private static readonly DateTime AutoCloseFromMonth = new DateTime(2026, 10, 1);

        public TreasuryService(ITreasuryRepository treasury, IBaseRepository<OwnBank> bank, IBankDashboardService dashboard)
        {
            _treasury = treasury;
            _bank = bank;
            _dashboard = dashboard;
        }

        public async Task<ServiceResult<TreasuryPageDto>> GetPageAsync(TreasuryFilterDto filter)
        {
            TreasuryEntryKind? kind = null;
            if (!string.IsNullOrWhiteSpace(filter.Kind))
            {
                if (!Enum.TryParse<TreasuryEntryKind>(filter.Kind, true, out var parsed) || !Enum.IsDefined(parsed))
                {
                    return ServiceResult<TreasuryPageDto>.Fail("Unknown entry type.");
                }
                kind = parsed;
            }

            // Tarixlər Bakı günüdür: UTC-yə çevirmək üçün 4 saat çıxılır, "to" günü də daxildir
            var from = ParseDay(filter.From, out var fromError);
            var to = ParseDay(filter.To, out var toError);
            if (fromError || toError)
            {
                return ServiceResult<TreasuryPageDto>.Fail("Choose dates like 2026-10-17.");
            }
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                return ServiceResult<TreasuryPageDto>.Fail("The start date can't be after the end date.");
            }

            var bank = await _bank.GetAsync();
            if (bank is null)
            {
                return ServiceResult<TreasuryPageDto>.Fail("The bank rules are not set up yet.");
            }

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 25 : Math.Min(filter.PageSize, MaxPageSize);
            var fromUtc = from.HasValue ? (DateTime?)DateTime.SpecifyKind(from.Value - BakuOffset, DateTimeKind.Utc) : null;
            var toUtc = to.HasValue ? (DateTime?)DateTime.SpecifyKind(to.Value.AddDays(1) - BakuOffset, DateTimeKind.Utc) : null;

            var (rows, total) = await _treasury.GetPagedAsync(kind, fromUtc, toUtc, page, pageSize);
            var balance = await _treasury.GetBalanceAsync();
            var customerMoney = await _treasury.GetCustomerMoneyAsync();
            var reserve = Math.Round(customerMoney * bank.ReservePercent / 100m, 2, MidpointRounding.AwayFromZero);

            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthStartUtc = DateTime.SpecifyKind(monthStart - BakuOffset, DateTimeKind.Utc);
            var monthEndUtc = DateTime.SpecifyKind(monthStart.AddMonths(1) - BakuOffset, DateTimeKind.Utc);
            var (monthIn, monthOut) = await _treasury.GetFlowAsync(monthStartUtc, monthEndUtc);

            return ServiceResult<TreasuryPageDto>.Ok(new TreasuryPageDto
            {
                Balance = balance,
                CustomerMoney = customerMoney,
                ReservePercent = bank.ReservePercent,
                RequiredReserve = reserve,
                AvailableForLoans = Math.Max(0m, balance - reserve),
                InDebt = balance < 0m,
                MonthIn = monthIn,
                MonthOut = monthOut,
                MonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                Items = rows.Select(r => new TreasuryEntryDto
                {
                    Id = r.Entry.Id,
                    CreatedAt = r.Entry.CreatedAt,
                    Kind = r.Entry.Kind.ToString(),
                    Description = r.Entry.Description,
                    Reference = r.Entry.Reference,
                    Amount = r.Entry.Amount,
                    BalanceAfter = r.BalanceAfter
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))
            });
        }

        public async Task<ServiceResult<TreasuryCloseDto>> CloseMonthAsync(string month)
        {
            if (!DateTime.TryParseExact((month ?? string.Empty).Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return ServiceResult<TreasuryCloseDto>.Fail("Choose a month like 2026-10.");
            }

            var monthStart = new DateTime(parsed.Year, parsed.Month, 1);
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            if (monthStart >= new DateTime(today.Year, today.Month, 1))
            {
                return ServiceResult<TreasuryCloseDto>.Fail("Only a finished month can be closed.");
            }

            return ServiceResult<TreasuryCloseDto>.Ok(await CloseAsync(monthStart));
        }

        public async Task<int> CloseDueMonthsAsync()
        {
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var previous = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            if (previous < AutoCloseFromMonth)
            {
                return 0;
            }

            // Proqram bağlı qalıbsa, buraxılmış aylar da bağlanır (köhnədən yeniyə)
            var closed = 0;
            for (var m = AutoCloseFromMonth; m <= previous; m = m.AddMonths(1))
            {
                var result = await CloseAsync(m);
                if (!result.AlreadyClosed)
                {
                    closed++;
                }
            }
            return closed;
        }

        // Ayın əməliyyat xərci və vergisi xəzinədən çıxış kimi yazılır; eyni ay təkrar bağlanmır (kodla yoxlanır)
        private async Task<TreasuryCloseDto> CloseAsync(DateTime monthStart)
        {
            var key = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var costRef = "CLOSE-" + key + "-OPEX";
            var taxRef = "CLOSE-" + key + "-TAX";

            if (await _treasury.ReferenceExistsAsync(costRef))
            {
                return new TreasuryCloseDto { Month = key, AlreadyClosed = true };
            }

            var costs = await _dashboard.GetMonthCostsAsync(monthStart);
            var operating = costs?.OperatingCosts ?? 0m;
            var tax = costs?.Tax ?? 0m;

            var entries = new List<TreasuryEntry>();
            if (operating > 0m)
            {
                entries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.OperatingCost, Amount = -operating, Reference = costRef, Description = "Operating costs for " + key });
            }
            if (tax > 0m)
            {
                entries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.ProfitTax, Amount = -tax, Reference = taxRef, Description = "Profit tax for " + key });
            }
            if (entries.Count == 0)
            {
                // Xərc də vergi də yoxdursa da ay "bağlanmış" sayılsın deyə sıfır sətir yazılır
                entries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.OperatingCost, Amount = 0m, Reference = costRef, Description = "Nothing to pay for " + key });
            }

            await _treasury.AddEntriesAsync(entries);
            return new TreasuryCloseDto { Month = key, OperatingCost = operating, ProfitTax = tax };
        }

        private static DateTime? ParseDay(string? text, out bool error)
        {
            error = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            if (!DateTime.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                error = true;
                return null;
            }
            return day.Date;
        }
    }
}
