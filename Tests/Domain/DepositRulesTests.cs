using Domain.Deposits;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class DepositRulesTests
    {
        [Theory]
        [InlineData(1000, 12, 6, 60.00)]
        [InlineData(1000, 12, 12, 120.00)]
        [InlineData(5000, 8.5, 3, 106.25)]
        [InlineData(100, 5.5, 3, 1.38)]      // 1.375 yuxarı yuvarlaqlaşır
        [InlineData(0, 10, 12, 0)]
        [InlineData(1000, 0, 12, 0)]
        [InlineData(1000, 10, 0, 0)]
        public void Interest_is_simple_interest_rounded_to_two_decimals(double principal, double rate, int months, double expected)
        {
            Assert.Equal((decimal)expected, DepositRules.Interest((decimal)principal, (decimal)rate, months));
        }

        [Fact]
        public void MaturityDate_adds_the_months()
        {
            var start = new DateTime(2026, 10, 21, 8, 30, 0, DateTimeKind.Utc);
            Assert.Equal(new DateTime(2027, 4, 21, 8, 30, 0, DateTimeKind.Utc), DepositRules.MaturityDate(start, 6));
        }

        [Fact]
        public void MaturityDate_clamps_to_the_last_day_of_a_short_month()
        {
            Assert.Equal(new DateTime(2026, 2, 28), DepositRules.MaturityDate(new DateTime(2026, 1, 31), 1));
        }

        [Fact]
        public void Progress_is_zero_at_start_half_in_the_middle_and_one_at_the_end()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 1, 11);
            Assert.Equal(0m, DepositRules.Progress(start, end, start));
            Assert.Equal(0.5m, DepositRules.Progress(start, end, new DateTime(2026, 1, 6)));
            Assert.Equal(1m, DepositRules.Progress(start, end, end));
        }

        [Fact]
        public void Progress_is_clamped_before_the_start_and_after_the_end()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 1, 11);
            Assert.Equal(0m, DepositRules.Progress(start, end, start.AddDays(-5)));
            Assert.Equal(1m, DepositRules.Progress(start, end, end.AddDays(5)));
        }

        [Fact]
        public void Progress_is_one_when_the_term_has_no_length()
        {
            var day = new DateTime(2026, 1, 1);
            Assert.Equal(1m, DepositRules.Progress(day, day, day));
            Assert.Equal(1m, DepositRules.Progress(day, day.AddDays(-1), day));
        }

        [Fact]
        public void MonthlyInterest_gives_the_rounded_share_and_the_last_month_takes_the_difference()
        {
            Assert.Equal(33.33m, DepositRules.MonthlyInterest(100m, 3, 1));
            Assert.Equal(33.33m, DepositRules.MonthlyInterest(100m, 3, 2));
            Assert.Equal(33.34m, DepositRules.MonthlyInterest(100m, 3, 3));
        }

        [Theory]
        [InlineData(100, 3)]
        [InlineData(60, 6)]
        [InlineData(106.25, 3)]
        [InlineData(1.38, 12)]
        [InlineData(1234.57, 24)]
        [InlineData(0.01, 6)]
        public void MonthlyInterest_always_adds_up_to_the_expected_interest(double expected, int term)
        {
            var total = 0m;
            for (var number = 1; number <= term; number++)
            {
                total += DepositRules.MonthlyInterest((decimal)expected, term, number);
            }
            Assert.Equal((decimal)expected, total);
        }

        [Fact]
        public void Accrued_is_the_share_of_the_expected_interest()
        {
            Assert.Equal(30m, DepositRules.Accrued(60m, 0.5m));
            Assert.Equal(0m, DepositRules.Accrued(60m, 0m));
            Assert.Equal(60m, DepositRules.Accrued(60m, 1m));
            Assert.Equal(0.33m, DepositRules.Accrued(1m, 0.333m));
        }
    }
}
