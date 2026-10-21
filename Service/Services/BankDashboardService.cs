using Domain.Entities;
using Domain.Loans;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class BankDashboardService : IBankDashboardService
    {
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        private readonly IBankDashboardRepository _dashboard;
        private readonly IBaseRepository<OwnBank> _ownBanks;

        public BankDashboardService(IBankDashboardRepository dashboard, IBaseRepository<OwnBank> ownBanks)
        {
            _dashboard = dashboard;
            _ownBanks = ownBanks;
        }

        // Bir ayın hesabı: gəlir, xərc, vergi (hamısı bazadakı real əməliyyatlardan, əməliyyat xərci və vergi faizi bankın qaydalarından)
        private sealed class MonthResult
        {
            public MonthMoney Money { get; set; } = new MonthMoney();
            public decimal Income { get; set; }
            public decimal OperatingCosts { get; set; }
            public decimal ProfitBeforeTax { get; set; }
            public decimal Tax { get; set; }
            public decimal Expenses { get; set; }
        }

        public async Task<ServiceResult<BankDashboardDto>> GetAsync(string? month)
        {
            var bank = await _ownBanks.GetAsync();
            if (bank is null)
            {
                return ServiceResult<BankDashboardDto>.Fail("The bank rules are not set up yet.");
            }

            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            var selected = currentMonth;
            if (!string.IsNullOrWhiteSpace(month))
            {
                if (!DateTime.TryParseExact(month.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return ServiceResult<BankDashboardDto>.Fail("Choose a month like 2026-10.");
                }
                selected = new DateTime(parsed.Year, parsed.Month, 1);
                if (selected > currentMonth)
                {
                    return ServiceResult<BankDashboardDto>.Fail("That month has not started yet.");
                }
            }

            var current = await ComputeMonthAsync(bank, selected);
            var previous = await ComputeMonthAsync(bank, selected.AddMonths(-1));

            // Rüb: seçilmiş ayın aid olduğu rübün başlanğıcından seçilmiş aya qədər yığılmış vergi
            var quarterStart = new DateTime(selected.Year, ((selected.Month - 1) / 3) * 3 + 1, 1);
            var quarterTax = 0m;
            for (var m = quarterStart; m <= selected; m = m.AddMonths(1))
            {
                quarterTax += m == selected ? current.Tax : (await ComputeMonthAsync(bank, m)).Tax;
            }

            var snapshot = await _dashboard.GetSnapshotAsync(ToUtc(selected), ToUtc(selected.AddMonths(1)));

            var required = Round((snapshot.CustomerDeposits + snapshot.TermDeposits) * bank.ReservePercent / 100m);
            var needed = required + quarterTax;
            var ownFunds = snapshot.TreasuryBalance;
            decimal? coverage = needed > 0m ? Math.Round(ownFunds / needed, 1) : null;

            var months = new List<DashboardMonthOptionDto>();
            for (var i = 0; i < 12; i++)
            {
                var item = currentMonth.AddMonths(-i);
                months.Add(new DashboardMonthOptionDto
                {
                    Value = item.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Label = item.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
                });
            }

            return ServiceResult<BankDashboardDto>.Ok(new BankDashboardDto
            {
                Period = selected.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                PeriodLabel = selected.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                Months = months,
                TotalUsers = snapshot.TotalUsers,
                NewUsers = snapshot.NewUsers,
                CustomerDeposits = snapshot.CustomerDeposits,
                TermDeposits = snapshot.TermDeposits,
                TermDepositInterestOwed = snapshot.TermDepositInterestOwed,
                LoansOutstanding = snapshot.LoansOutstanding,
                TransferCommissions = current.Money.Commissions,
                CardOpeningFees = current.Money.CardFees,
                LoanInterest = current.Money.LoanInterest,
                LatePenalties = current.Money.Penalties,
                TotalIncome = current.Income,
                IncomeChangePercent = Change(current.Income, previous.Income),
                CashbackPaid = current.Money.Cashback,
                DepositInterestPaid = current.Money.DepositInterest,
                OperatingCosts = current.OperatingCosts,
                ProfitTaxPercent = bank.ProfitTaxPercent,
                ProfitTax = current.Tax,
                TotalExpenses = current.Expenses,
                ExpenseChangePercent = Change(current.Expenses, previous.Expenses),
                ProfitBeforeTax = current.ProfitBeforeTax,
                NetProfit = Round(current.Income - current.Expenses),
                QuarterTaxAccrued = quarterTax,
                ReservePercent = bank.ReservePercent,
                RequiredReserve = required,
                OwnFunds = ownFunds,
                ReserveAfterTax = Round(ownFunds - quarterTax),
                CoverageRatio = coverage,
                ReserveHealthy = ownFunds >= needed,
                PendingLoans = snapshot.PendingLoans,
                PendingTickets = snapshot.PendingTickets,
                RestrictedAccounts = snapshot.RestrictedUsers
            });
        }

        public async Task<(decimal OperatingCosts, decimal Tax)?> GetMonthCostsAsync(DateTime monthStart)
        {
            var bank = await _ownBanks.GetAsync();
            if (bank is null)
            {
                return null;
            }
            var result = await ComputeMonthAsync(bank, new DateTime(monthStart.Year, monthStart.Month, 1));
            return (result.OperatingCosts, result.Tax);
        }

        private async Task<MonthResult> ComputeMonthAsync(OwnBank bank, DateTime monthStart)
        {
            var money = await _dashboard.GetMonthMoneyAsync(ToUtc(monthStart), ToUtc(monthStart.AddMonths(1)));
            var income = Round(money.Commissions + money.CardFees + money.LoanInterest + money.Penalties);

            // Vergi mənfəətdən tutulur (gəlir - cashback - depozit faizi - əməliyyat xərci). Zərər olanda vergi yoxdur
            var profitBeforeTax = Round(income - money.Cashback - money.DepositInterest - bank.OperatingCostMonthly);
            var tax = profitBeforeTax > 0m ? Round(profitBeforeTax * bank.ProfitTaxPercent / 100m) : 0m;

            return new MonthResult
            {
                Money = money,
                Income = income,
                OperatingCosts = bank.OperatingCostMonthly,
                ProfitBeforeTax = profitBeforeTax,
                Tax = tax,
                Expenses = Round(money.Cashback + money.DepositInterest + bank.OperatingCostMonthly + tax)
            };
        }

        // Bakı ayının başlanğıcı UTC-də (Bakı = UTC + 4)
        private static DateTime ToUtc(DateTime bakuMonthStart)
        {
            return DateTime.SpecifyKind(bakuMonthStart - BakuOffset, DateTimeKind.Utc);
        }

        private static decimal? Change(decimal current, decimal previous)
        {
            if (previous <= 0m)
            {
                return null;
            }
            return Math.Round((current - previous) / previous * 100m, 1);
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
