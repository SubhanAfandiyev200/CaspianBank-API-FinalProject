using Domain.Bills;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class RecurringRulesTests
    {
        [Fact]
        public void Bills_are_due_any_day_of_a_new_month()
        {
            Assert.True(RecurringRules.IsDue(false, new DateTime(2026, 10, 1), "2026-09"));
            Assert.True(RecurringRules.IsDue(false, new DateTime(2026, 10, 17), "2026-09"));
        }

        [Fact]
        public void Bills_are_not_due_twice_in_the_same_month()
        {
            Assert.False(RecurringRules.IsDue(false, new DateTime(2026, 10, 17), "2026-10"));
        }

        [Fact]
        public void Loans_are_due_only_on_the_last_day_of_the_month()
        {
            Assert.False(RecurringRules.IsDue(true, new DateTime(2026, 10, 1), "2026-09"));
            Assert.False(RecurringRules.IsDue(true, new DateTime(2026, 10, 30), "2026-09"));
            Assert.True(RecurringRules.IsDue(true, new DateTime(2026, 10, 31), "2026-09"));
        }

        [Fact]
        public void Loans_use_the_real_last_day_of_short_months()
        {
            Assert.True(RecurringRules.IsDue(true, new DateTime(2026, 2, 28), "2026-01"));
            Assert.True(RecurringRules.IsDue(true, new DateTime(2028, 2, 29), "2028-01"));
            Assert.False(RecurringRules.IsDue(true, new DateTime(2028, 2, 28), "2028-01"));
        }

        [Fact]
        public void Loans_are_due_at_once_if_a_previous_month_was_skipped()
        {
            Assert.True(RecurringRules.IsDue(true, new DateTime(2026, 10, 5), "2026-08"));
        }

        [Fact]
        public void Loans_are_not_due_again_after_the_month_was_processed()
        {
            Assert.False(RecurringRules.IsDue(true, new DateTime(2026, 10, 31), "2026-10"));
        }

        [Fact]
        public void NextRun_is_today_when_due()
        {
            Assert.Equal(new DateTime(2026, 10, 17), RecurringRules.NextRun(false, new DateTime(2026, 10, 17, 14, 0, 0), "2026-09"));
        }

        [Fact]
        public void NextRun_of_a_bill_is_the_first_of_next_month()
        {
            Assert.Equal(new DateTime(2026, 11, 1), RecurringRules.NextRun(false, new DateTime(2026, 10, 17), "2026-10"));
        }

        [Fact]
        public void NextRun_of_a_loan_is_the_last_day_of_this_month_when_not_yet_processed()
        {
            Assert.Equal(new DateTime(2026, 10, 31), RecurringRules.NextRun(true, new DateTime(2026, 10, 17), "2026-09"));
        }

        [Fact]
        public void NextRun_of_a_loan_is_the_last_day_of_next_month_when_this_month_is_done()
        {
            Assert.Equal(new DateTime(2026, 11, 30), RecurringRules.NextRun(true, new DateTime(2026, 10, 17), "2026-10"));
        }

        [Fact]
        public void StartingPeriod_of_a_bill_is_this_month_and_of_a_loan_last_month()
        {
            Assert.Equal("2026-10", RecurringRules.StartingPeriod(false, new DateTime(2026, 10, 17)));
            Assert.Equal("2026-09", RecurringRules.StartingPeriod(true, new DateTime(2026, 10, 17)));
            Assert.Equal("2025-12", RecurringRules.StartingPeriod(true, new DateTime(2026, 1, 17)));
        }
    }
}
