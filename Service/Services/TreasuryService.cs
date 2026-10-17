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

        public TreasuryService(ITreasuryRepository treasury, IBaseRepository<OwnBank> bank)
        {
            _treasury = treasury;
            _bank = bank;
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
