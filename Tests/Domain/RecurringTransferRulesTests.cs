using Domain.Transfers;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class RecurringTransferRulesTests
    {
        [Fact]
        public void PeriodOf_formats_year_and_month()
        {
            Assert.Equal("2026-10", RecurringTransferRules.PeriodOf(new DateTime(2026, 10, 21)));
            Assert.Equal("2026-01", RecurringTransferRules.PeriodOf(new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void InitialLastPeriod_is_this_month_when_the_day_has_passed_or_is_today()
        {
            Assert.Equal("2026-10", RecurringTransferRules.InitialLastPeriod(new DateTime(2026, 10, 10), 5));
            Assert.Equal("2026-10", RecurringTransferRules.InitialLastPeriod(new DateTime(2026, 10, 10), 10));
        }

        [Fact]
        public void InitialLastPeriod_is_last_month_when_the_day_is_still_ahead()
        {
            Assert.Equal("2026-09", RecurringTransferRules.InitialLastPeriod(new DateTime(2026, 10, 10), 15));
        }

        [Fact]
        public void InitialLastPeriod_wraps_over_the_new_year()
        {
            Assert.Equal("2025-12", RecurringTransferRules.InitialLastPeriod(new DateTime(2026, 1, 3), 10));
        }

        [Fact]
        public void IsDue_when_the_day_has_come_and_this_month_is_not_done()
        {
            Assert.True(RecurringTransferRules.IsDue(new DateTime(2026, 10, 10), 5, "2026-09"));
            Assert.True(RecurringTransferRules.IsDue(new DateTime(2026, 10, 5), 5, "2026-09"));
        }

        [Fact]
        public void IsDue_is_false_when_this_month_is_already_done()
        {
            Assert.False(RecurringTransferRules.IsDue(new DateTime(2026, 10, 10), 5, "2026-10"));
        }

        [Fact]
        public void IsDue_is_false_before_the_chosen_day()
        {
            Assert.False(RecurringTransferRules.IsDue(new DateTime(2026, 10, 3), 5, "2026-09"));
        }

        [Fact]
        public void IsDue_catches_up_later_in_the_month_if_the_app_was_off()
        {
            Assert.True(RecurringTransferRules.IsDue(new DateTime(2026, 10, 27), 5, "2026-09"));
        }

        [Fact]
        public void NextRun_is_today_when_due()
        {
            var today = new DateTime(2026, 10, 10);
            Assert.Equal(today, RecurringTransferRules.NextRun(today, 5, "2026-09"));
        }

        [Fact]
        public void NextRun_is_the_chosen_day_of_this_month_when_it_is_still_ahead()
        {
            Assert.Equal(new DateTime(2026, 10, 15), RecurringTransferRules.NextRun(new DateTime(2026, 10, 10), 15, "2026-09"));
        }

        [Fact]
        public void NextRun_is_next_month_when_this_month_is_done()
        {
            Assert.Equal(new DateTime(2026, 11, 5), RecurringTransferRules.NextRun(new DateTime(2026, 10, 10), 5, "2026-10"));
            Assert.Equal(new DateTime(2027, 1, 5), RecurringTransferRules.NextRun(new DateTime(2026, 12, 10), 5, "2026-12"));
        }

        [Fact]
        public void Day_limits_avoid_days_that_some_months_do_not_have()
        {
            Assert.Equal(1, RecurringTransferRules.MinDay);
            Assert.Equal(28, RecurringTransferRules.MaxDay);
        }
    }
}
