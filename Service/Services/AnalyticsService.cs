using Domain.Enums;
using Domain.Loans;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Analytics;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        // Kateqoriyalar və göstərilən adlar (sıra qrafikdə də belədir)
        private static readonly (string Code, string Name)[] Categories =
        {
            ("Transfers", "Transfers to others"),
            ("Bills", "Bill payments"),
            ("Loans", "Loan payments"),
            ("Fees", "Fees and commissions"),
            ("Deposits", "Put into deposits")
        };

        private readonly IAnalyticsRepository _analytics;

        public AnalyticsService(IAnalyticsRepository analytics)
        {
            _analytics = analytics;
        }

        public async Task<SpendingAnalyticsDto> GetSpendingAsync(string userId, int months)
        {
            var count = Math.Clamp(months, 1, 12);
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var currentStart = new DateTime(today.Year, today.Month, 1);
            var firstStart = currentStart.AddMonths(-(count - 1));
            var fromUtc = DateTime.SpecifyKind(firstStart - BakuOffset, DateTimeKind.Utc);

            var rows = await _analytics.GetOutgoingAsync(userId, fromUtc);

            var result = new SpendingAnalyticsDto();
            for (var i = 0; i < count; i++)
            {
                var start = firstStart.AddMonths(i);
                var key = start.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var month = new SpendingMonthDto { Key = key, Label = start.ToString("MMM yyyy", CultureInfo.InvariantCulture) };

                foreach (var (code, name) in Categories)
                {
                    var amount = rows
                        .Where(r => CodeOf(r.Type) == code && (r.CreatedAt + BakuOffset).ToString("yyyy-MM", CultureInfo.InvariantCulture) == key)
                        .Sum(r => r.Amount);
                    month.Categories.Add(new SpendingCategoryDto { Code = code, Name = name, Amount = amount });
                }
                month.Total = month.Categories.Sum(c => c.Amount);
                result.Months.Add(month);
            }

            var current = result.Months[result.Months.Count - 1];
            var previous = result.Months.Count > 1 ? result.Months[result.Months.Count - 2] : null;
            result.CurrentTotal = current.Total;
            result.PreviousTotal = previous?.Total ?? 0m;
            if (previous is not null && previous.Total > 0m)
            {
                result.ChangePercent = Math.Round((current.Total - previous.Total) / previous.Total * 100m, 1);
            }
            var top = current.Categories.OrderByDescending(c => c.Amount).FirstOrDefault();
            result.TopCategory = top is not null && top.Amount > 0m ? top.Name : null;
            result.AverageMonthly = Math.Round(result.Months.Average(m => m.Total), 2);
            return result;
        }

        private static string CodeOf(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.TransferOut:
                    return "Transfers";
                case TransactionType.BillPayment:
                    return "Bills";
                case TransactionType.LoanPayment:
                    return "Loans";
                case TransactionType.DepositOpen:
                    return "Deposits";
                default:
                    return "Fees";
            }
        }
    }
}
