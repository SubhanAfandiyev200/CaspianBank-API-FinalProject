using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Xunit;

namespace CaspianBank.Tests.DomainRules
{
    public class LoanEngineTests
    {
        // ---------- Köməkçilər ----------

        private static OwnBank Bank()
        {
            return new OwnBank
            {
                MinEarlyPayment = 10m,
                EarlyRepaymentMinPercent = 10m,
                EarlyRepaymentRateReduction = 1m,
                MinAnnualInterestRate = 5m
            };
        }

        // Aktivləşdirilmiş kredit (pul 2026-10-15 verilib: ilk ödəniş 2026-11-30)
        private static Loan ActiveLoan(decimal principal, decimal rate, int months)
        {
            var loan = new Loan
            {
                Id = 1,
                RequestedAmount = principal,
                ApprovedAmount = principal,
                TermMonths = months,
                AnnualInterestRate = rate,
                InitialAnnualInterestRate = rate,
                FixedPenalty = 10m,
                DailyPenaltyPercent = 0.5m,
                Status = LoanStatus.Pending
            };
            LoanEngine.Activate(loan, new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc));
            return loan;
        }

        private static LoanInstallment Installment(int number, DateTime due, decimal principal, decimal interest)
        {
            return new LoanInstallment { Id = number, LoanId = 1, Number = number, DueDate = due, PrincipalAmount = principal, InterestAmount = interest };
        }

        // 3 aylıq ödəniş: 2026-09-30, 2026-10-31, 2026-11-30, hər biri 100 əsas borc + 10 faiz
        private static Loan ThreeInstallmentLoan()
        {
            var loan = new Loan { Id = 1, Status = LoanStatus.Active, FixedPenalty = 10m, DailyPenaltyPercent = 0.5m, AnnualInterestRate = 10m };
            loan.Installments.Add(Installment(1, new DateTime(2026, 9, 30), 100m, 10m));
            loan.Installments.Add(Installment(2, new DateTime(2026, 10, 31), 100m, 10m));
            loan.Installments.Add(Installment(3, new DateTime(2026, 11, 30), 100m, 10m));
            return loan;
        }

        // ---------- Tarix köməkçiləri ----------

        [Fact]
        public void BakuToday_is_utc_plus_four()
        {
            Assert.Equal(new DateTime(2026, 10, 22), LoanEngine.BakuToday(new DateTime(2026, 10, 21, 20, 30, 0, DateTimeKind.Utc)));
            Assert.Equal(new DateTime(2026, 10, 21), LoanEngine.BakuToday(new DateTime(2026, 10, 21, 19, 59, 0, DateTimeKind.Utc)));
        }

        [Theory]
        [InlineData(2026, 10, 31)]
        [InlineData(2026, 11, 30)]
        [InlineData(2026, 2, 28)]
        [InlineData(2028, 2, 29)]
        public void LastDayOfMonth_is_correct(int year, int month, int expectedDay)
        {
            Assert.Equal(new DateTime(year, month, expectedDay), LoanEngine.LastDayOfMonth(new DateTime(year, month, 1)));
        }

        [Fact]
        public void FirstDueDate_is_the_last_day_of_the_month_after_disbursement()
        {
            Assert.Equal(new DateTime(2026, 11, 30), LoanEngine.FirstDueDate(new DateTime(2026, 10, 15)));
            Assert.Equal(new DateTime(2026, 11, 30), LoanEngine.FirstDueDate(new DateTime(2026, 10, 31)));
            Assert.Equal(new DateTime(2027, 1, 31), LoanEngine.FirstDueDate(new DateTime(2026, 12, 1)));
            Assert.Equal(new DateTime(2026, 2, 28), LoanEngine.FirstDueDate(new DateTime(2026, 1, 31)));
        }

        [Theory]
        [InlineData("2000-10-21", "2026-10-21", 26)]
        [InlineData("2000-10-22", "2026-10-21", 25)]
        [InlineData("2008-10-21", "2026-10-21", 18)]
        [InlineData("2008-10-22", "2026-10-21", 17)]
        [InlineData("2000-02-29", "2026-02-28", 25)]
        public void AgeOn_counts_full_years(string birth, string date, int expected)
        {
            Assert.Equal(expected, LoanEngine.AgeOn(DateTime.Parse(birth), DateTime.Parse(date)));
        }

        // ---------- Annuitet ----------

        [Fact]
        public void MonthlyPayment_matches_the_annuity_formula()
        {
            Assert.Equal(888.49m, LoanEngine.MonthlyPayment(10000m, 12m, 12));
        }

        [Fact]
        public void MonthlyPayment_without_interest_is_an_equal_split()
        {
            Assert.Equal(100m, LoanEngine.MonthlyPayment(1200m, 0m, 12));
        }

        [Fact]
        public void MonthlyPayment_is_zero_for_no_months()
        {
            Assert.Equal(0m, LoanEngine.MonthlyPayment(1000m, 10m, 0));
            Assert.Equal(0m, LoanEngine.MonthlyPayment(1000m, 10m, -3));
        }

        [Theory]
        [InlineData(500, 10, 6)]
        [InlineData(10000, 12, 12)]
        [InlineData(50000, 10, 48)]
        [InlineData(2500.50, 9.5, 18)]
        public void PrincipalFromPayment_is_the_inverse_of_MonthlyPayment(double principal, double rate, int months)
        {
            var payment = LoanEngine.MonthlyPayment((decimal)principal, (decimal)rate, months);
            var back = LoanEngine.PrincipalFromPayment(payment, (decimal)rate, months);
            Assert.InRange(Math.Abs(back - (decimal)principal), 0m, 1m);
        }

        [Fact]
        public void PrincipalFromPayment_handles_zero_rate_and_empty_input()
        {
            Assert.Equal(1200m, LoanEngine.PrincipalFromPayment(100m, 0m, 12));
            Assert.Equal(0m, LoanEngine.PrincipalFromPayment(0m, 10m, 12));
            Assert.Equal(0m, LoanEngine.PrincipalFromPayment(100m, 10m, 0));
        }

        [Theory]
        [InlineData(1000, 10, 6)]
        [InlineData(10000, 12, 12)]
        [InlineData(50000, 10, 48)]
        [InlineData(777.77, 7.3, 7)]
        [InlineData(1200, 0, 12)]
        public void BuildSchedule_repays_exactly_the_principal(double principal, double rate, int months)
        {
            var lines = LoanEngine.BuildSchedule((decimal)principal, (decimal)rate, months, new DateTime(2026, 11, 30));

            Assert.Equal(months, lines.Count);
            Assert.Equal((decimal)principal, lines.Sum(l => l.PrincipalAmount));
            Assert.All(lines, l => Assert.True(l.PrincipalAmount >= 0m && l.InterestAmount >= 0m));
            Assert.Equal(Enumerable.Range(1, months), lines.Select(l => l.Number));
        }

        [Fact]
        public void BuildSchedule_due_dates_are_month_ends()
        {
            var lines = LoanEngine.BuildSchedule(1200m, 10m, 4, new DateTime(2027, 1, 31));
            Assert.Equal(new[]
            {
                new DateTime(2027, 1, 31), new DateTime(2027, 2, 28), new DateTime(2027, 3, 31), new DateTime(2027, 4, 30)
            }, lines.Select(l => l.DueDate).ToArray());
        }

        [Fact]
        public void BuildSchedule_first_interest_is_on_the_whole_balance()
        {
            var lines = LoanEngine.BuildSchedule(12000m, 12m, 12, new DateTime(2026, 11, 30));
            Assert.Equal(120m, lines[0].InterestAmount);          // 12000 * 1%
            Assert.True(lines[1].InterestAmount < lines[0].InterestAmount);
        }

        [Fact]
        public void BuildSchedule_without_interest_has_no_interest()
        {
            var lines = LoanEngine.BuildSchedule(1200m, 0m, 12, new DateTime(2026, 11, 30));
            Assert.All(lines, l => Assert.Equal(0m, l.InterestAmount));
            Assert.All(lines, l => Assert.Equal(100m, l.PrincipalAmount));
        }

        [Fact]
        public void Quote_totals_are_consistent()
        {
            var quote = LoanEngine.Quote(10000m, 12m, 12);
            Assert.Equal(888.49m, quote.MonthlyPayment);
            Assert.Equal(quote.TotalRepayment - 10000m, quote.TotalInterest);
            Assert.InRange(Math.Abs(quote.TotalRepayment - quote.MonthlyPayment * 12), 0m, 1m);
        }

        // ---------- Məbləğ qaydaları ----------

        [Theory]
        [InlineData(0, 24)]
        [InlineData(-1, 24)]
        [InlineData(1, 12)]
        [InlineData(2, 12)]
        [InlineData(3, 4)]
        [InlineData(10, 4)]
        public void DelayMultiplier_depends_on_delays_in_other_banks(int delays, int expected)
        {
            Assert.Equal(expected, LoanEngine.DelayMultiplier(delays));
        }

        [Fact]
        public void MaxByIncome_is_the_smaller_of_the_bank_maximum_and_income_times_multiplier()
        {
            Assert.Equal(24000m, LoanEngine.MaxByIncome(1000m, 0, 50000m));
            Assert.Equal(12000m, LoanEngine.MaxByIncome(1000m, 2, 50000m));
            Assert.Equal(4000m, LoanEngine.MaxByIncome(1000m, 5, 50000m));
            Assert.Equal(50000m, LoanEngine.MaxByIncome(10000m, 0, 50000m));
            Assert.Equal(1234m, LoanEngine.MaxByIncome(51.4167m, 0, 50000m));      // aşağı yuvarlaqlaşır
        }

        [Fact]
        public void MaxByPaymentLimit_uses_what_is_left_of_the_payment_limit()
        {
            var expected = Math.Floor(LoanEngine.PrincipalFromPayment(450m, 12m, 12));
            Assert.Equal(expected, LoanEngine.MaxByPaymentLimit(1000m, 45m, 0m, 12m, 12));
        }

        [Fact]
        public void MaxByPaymentLimit_is_zero_when_existing_payments_use_the_whole_limit()
        {
            Assert.Equal(0m, LoanEngine.MaxByPaymentLimit(1000m, 45m, 450m, 12m, 12));
            Assert.Equal(0m, LoanEngine.MaxByPaymentLimit(1000m, 45m, 900m, 12m, 12));
        }

        // ---------- Aylıq ödənişin vəziyyəti ----------

        [Fact]
        public void Installment_balances_pay_interest_first_then_principal()
        {
            var installment = Installment(1, new DateTime(2026, 11, 30), 100m, 10m);
            installment.PaidAmount = 4m;
            Assert.Equal(6m, LoanEngine.InterestLeft(installment));
            Assert.Equal(100m, LoanEngine.PrincipalLeft(installment));

            installment.PaidAmount = 50m;
            Assert.Equal(0m, LoanEngine.InterestLeft(installment));
            Assert.Equal(60m, LoanEngine.PrincipalLeft(installment));

            installment.PaidAmount = 110m;
            Assert.Equal(0m, LoanEngine.PrincipalLeft(installment));
            Assert.True(LoanEngine.IsSettled(installment));
        }

        [Fact]
        public void PenaltyLeft_is_fixed_plus_accrued_minus_paid_and_never_negative()
        {
            var installment = Installment(1, new DateTime(2026, 11, 30), 100m, 10m);
            installment.FixedPenaltyAmount = 10m;
            installment.AccruedPenalty = 5m;
            installment.PaidPenalty = 4m;
            Assert.Equal(11m, LoanEngine.PenaltyLeft(installment));

            installment.PaidPenalty = 20m;
            Assert.Equal(0m, LoanEngine.PenaltyLeft(installment));
        }

        [Fact]
        public void An_unpaid_penalty_keeps_the_installment_open()
        {
            var installment = Installment(1, new DateTime(2026, 11, 30), 100m, 10m);
            installment.PaidAmount = 110m;
            installment.FixedPenaltyAmount = 10m;
            Assert.False(LoanEngine.IsSettled(installment));
        }

        [Fact]
        public void A_cancelled_installment_is_settled()
        {
            var installment = Installment(1, new DateTime(2026, 11, 30), 100m, 10m);
            installment.IsCancelled = true;
            Assert.True(LoanEngine.IsSettled(installment));
        }

        // ---------- Cərimə ----------

        [Fact]
        public void AccrueAll_adds_the_fixed_penalty_and_the_daily_percent()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));      // 1-ci ödəniş 10 gün gecikib

            var first = loan.Installments.First(i => i.Number == 1);
            Assert.Equal(10m, first.FixedPenaltyAmount);
            Assert.Equal(5.50m, first.AccruedPenalty);                   // 110 x 0.5% x 10 gün
            Assert.Equal(new DateTime(2026, 10, 10), first.PenaltyUpdatedAt);
        }

        [Fact]
        public void AccrueAll_does_not_touch_installments_that_are_not_late()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));

            Assert.Equal(0m, loan.Installments.First(i => i.Number == 2).AccruedPenalty);
            Assert.Null(loan.Installments.First(i => i.Number == 2).PenaltyUpdatedAt);
        }

        [Fact]
        public void AccrueAll_is_not_late_on_the_due_date_itself()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.AccrueAll(loan, new DateTime(2026, 9, 30));
            Assert.Equal(0m, loan.Installments.First(i => i.Number == 1).FixedPenaltyAmount);
        }

        [Fact]
        public void AccrueAll_twice_on_the_same_day_does_not_add_more()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));
            Assert.Equal(5.50m, loan.Installments.First(i => i.Number == 1).AccruedPenalty);
        }

        [Fact]
        public void AccrueAll_continues_from_the_last_accrual_day_and_does_not_charge_penalty_on_penalty()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 11));

            var first = loan.Installments.First(i => i.Number == 1);
            Assert.Equal(6.05m, first.AccruedPenalty);                   // 5.50 + 110 x 0.5% x 1
            Assert.Equal(10m, first.FixedPenaltyAmount);                 // sabit cərimə təkrar yazılmır
        }

        [Fact]
        public void AccrueAll_ignores_installments_that_were_paid_on_time()
        {
            var loan = ThreeInstallmentLoan();
            var first = loan.Installments.First(i => i.Number == 1);
            first.PaidAmount = 110m;
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));

            Assert.Equal(0m, first.FixedPenaltyAmount);
            Assert.Equal(0m, first.AccruedPenalty);
            Assert.Null(first.PenaltyUpdatedAt);
        }

        [Fact]
        public void AccrueAll_stops_the_daily_part_once_the_installment_is_paid_late_but_keeps_the_penalty_owed()
        {
            var loan = ThreeInstallmentLoan();
            var first = loan.Installments.First(i => i.Number == 1);
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));
            first.PaidAmount = 110m;                                      // faiz və əsas borc ödənildi, cərimə qaldı
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 20));

            Assert.Equal(5.50m, first.AccruedPenalty);
            Assert.Equal(15.50m, LoanEngine.PenaltyLeft(first));
        }

        [Fact]
        public void AccrueAll_skips_cancelled_installments()
        {
            var loan = ThreeInstallmentLoan();
            var first = loan.Installments.First(i => i.Number == 1);
            first.IsCancelled = true;
            LoanEngine.AccrueAll(loan, new DateTime(2026, 10, 10));
            Assert.Equal(0m, first.FixedPenaltyAmount);
        }

        // ---------- İndi ödənilməli məbləğ ----------

        [Fact]
        public void GetDue_lists_the_overdue_installment_and_only_the_next_upcoming_one()
        {
            var loan = ThreeInstallmentLoan();
            var due = LoanEngine.GetDue(loan, new DateTime(2026, 10, 10));

            Assert.Equal(2, due.Items.Count);
            Assert.True(due.Items[0].IsOverdue);
            Assert.Equal(10, due.Items[0].DaysOverdue);
            Assert.False(due.Items[1].IsOverdue);
            Assert.Equal(1, due.OverdueCount);
            Assert.True(due.HasOverdue);
        }

        [Fact]
        public void GetDue_adds_penalty_interest_and_principal()
        {
            var loan = ThreeInstallmentLoan();
            var due = LoanEngine.GetDue(loan, new DateTime(2026, 10, 10));

            Assert.Equal(15.50m, due.PenaltyTotal);              // 10 + 5.50
            Assert.Equal(20m, due.InterestTotal);                // 10 + 10
            Assert.Equal(200m, due.PrincipalTotal);              // 100 + 100
            Assert.Equal(235.50m, due.Total);
            Assert.Equal(125.50m, due.OverdueTotal);
        }

        [Fact]
        public void GetDue_with_nothing_late_shows_only_the_next_installment()
        {
            var loan = ThreeInstallmentLoan();
            loan.Installments.First(i => i.Number == 1).PaidAmount = 110m;
            var due = LoanEngine.GetDue(loan, new DateTime(2026, 10, 10));

            Assert.Single(due.Items);
            Assert.False(due.HasOverdue);
            Assert.Equal(110m, due.Total);
        }

        [Fact]
        public void GetDue_is_empty_when_everything_is_paid()
        {
            var loan = ThreeInstallmentLoan();
            foreach (var installment in loan.Installments)
            {
                installment.PaidAmount = 110m;
            }
            var due = LoanEngine.GetDue(loan, new DateTime(2026, 12, 1));
            Assert.Empty(due.Items);
            Assert.Equal(0m, due.Total);
        }

        // ---------- Aylıq ödənişin ödənilməsi ----------

        [Fact]
        public void ApplyInstallmentPayment_pays_everything_that_is_due()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 10, 10);
            var due = LoanEngine.GetDue(loan, asOf);

            var records = LoanEngine.ApplyInstallmentPayment(loan, due, asOf, "4169000000000001", "LP-1");

            Assert.Equal(2, records.Count);
            Assert.Equal(due.Total, records.Sum(r => r.Amount));
            Assert.All(records, r => Assert.Equal(LoanPaymentType.Installment, r.Type));
            Assert.All(records, r => Assert.Equal("LP-1", r.TransactionReference));
            Assert.True(LoanEngine.IsSettled(loan.Installments.First(i => i.Number == 1)));
            Assert.True(LoanEngine.IsSettled(loan.Installments.First(i => i.Number == 2)));
            Assert.False(LoanEngine.IsSettled(loan.Installments.First(i => i.Number == 3)));
            Assert.Equal(new DateTime(2026, 10, 10), loan.Installments.First(i => i.Number == 1).FullyPaidAt);
        }

        [Fact]
        public void ApplyInstallmentPayment_record_parts_add_up_to_the_amount()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 10, 10);
            var due = LoanEngine.GetDue(loan, asOf);
            var records = LoanEngine.ApplyInstallmentPayment(loan, due, asOf, "card", "LP-2");

            Assert.All(records, r => Assert.Equal(r.Amount, r.PenaltyPart + r.InterestPart + r.PrincipalPart));
            Assert.Equal(15.50m, records.Sum(r => r.PenaltyPart));
        }

        [Fact]
        public void ApplyInstallmentPayment_part_amount_pays_penalty_then_interest_then_principal_oldest_first()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 10, 10);
            var due = LoanEngine.GetDue(loan, asOf);

            // Gecikmiş ödəniş: cərimə 15.50 + faiz 10 + əsas borc 100. 30 AZN: cərimə + 14.50 faizdən
            var records = LoanEngine.ApplyInstallmentPayment(loan, due, asOf, "card", "LP-3", 30m);

            Assert.Single(records);
            Assert.Equal(15.50m, records[0].PenaltyPart);
            Assert.Equal(10m, records[0].InterestPart);
            Assert.Equal(4.50m, records[0].PrincipalPart);
            var first = loan.Installments.First(i => i.Number == 1);
            Assert.False(LoanEngine.IsSettled(first));
            Assert.Equal(0m, LoanEngine.PenaltyLeft(first));
            Assert.Equal(95.50m, LoanEngine.PrincipalLeft(first));
            Assert.Equal(0m, loan.Installments.First(i => i.Number == 2).PaidAmount);
        }

        [Fact]
        public void ApplyInstallmentPayment_amount_equal_or_above_the_debt_pays_everything()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 10, 10);
            var due = LoanEngine.GetDue(loan, asOf);
            var records = LoanEngine.ApplyInstallmentPayment(loan, due, asOf, "card", "LP-4", due.Total);
            Assert.Equal(due.Total, records.Sum(r => r.Amount));
        }

        [Fact]
        public void ApplyInstallmentPayment_closes_the_loan_when_the_last_installment_is_paid()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 12, 1);
            loan.Installments.First(i => i.Number == 1).PaidAmount = 110m;
            loan.Installments.First(i => i.Number == 2).PaidAmount = 110m;
            var due = LoanEngine.GetDue(loan, asOf);                    // 3-cü ödəniş 1 gün gecikib
            LoanEngine.ApplyInstallmentPayment(loan, due, asOf, "card", "LP-5");

            Assert.Equal(LoanStatus.Closed, loan.Status);
            Assert.Equal(asOf, loan.ClosedAt);
            Assert.Equal(0m, loan.OutstandingPrincipal);
        }

        // ---------- Vaxtından əvvəl hissəvi ödəniş ----------

        [Fact]
        public void CheckPartialEarly_rejects_a_loan_that_is_not_active()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            loan.Status = LoanStatus.Closed;
            Assert.Equal("Only an active loan can be paid early.", LoanEngine.CheckPartialEarly(loan, 100m, new DateTime(2026, 11, 10), Bank()));
        }

        [Fact]
        public void CheckPartialEarly_rejects_when_something_is_overdue()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            Assert.Equal("Pay the overdue installments first.", LoanEngine.CheckPartialEarly(loan, 100m, new DateTime(2026, 12, 5), Bank()));
        }

        [Fact]
        public void CheckPartialEarly_rejects_below_the_minimum_amount()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            var message = LoanEngine.CheckPartialEarly(loan, 5m, new DateTime(2026, 11, 10), Bank());
            Assert.Equal("The minimum early payment is 10.00 AZN.", message);
        }

        [Fact]
        public void CheckPartialEarly_rejects_an_amount_that_covers_the_whole_balance()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            var message = LoanEngine.CheckPartialEarly(loan, 1200m, new DateTime(2026, 11, 10), Bank());
            Assert.StartsWith("This amount covers the whole remaining balance", message);
        }

        [Fact]
        public void CheckPartialEarly_requires_the_current_installment_to_be_paid_first_if_it_is_part_paid()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            loan.Installments.First(i => i.Number == 1).PaidAmount = 5m;
            Assert.Equal("Pay the current installment first.", LoanEngine.CheckPartialEarly(loan, 100m, new DateTime(2026, 11, 10), Bank()));
        }

        [Fact]
        public void CheckPartialEarly_accepts_a_valid_amount()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            Assert.Null(LoanEngine.CheckPartialEarly(loan, 100m, new DateTime(2026, 11, 10), Bank()));
        }

        [Fact]
        public void CheckPartialEarly_rejects_a_loan_with_nothing_left()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            foreach (var installment in loan.Installments)
            {
                installment.IsCancelled = true;
            }
            Assert.Equal("This loan has nothing left to pay.", LoanEngine.CheckPartialEarly(loan, 100m, new DateTime(2026, 11, 10), Bank()));
        }

        [Fact]
        public void ApplyPartialEarly_lowers_the_balance_and_the_monthly_payment_and_keeps_the_months()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            var paymentBefore = loan.MonthlyPayment;

            var record = LoanEngine.ApplyPartialEarly(loan, 300m, new DateTime(2026, 11, 10), "card", "LP-6", Bank());

            Assert.Equal(LoanPaymentType.PartialEarly, record.Type);
            Assert.Equal(300m, record.Amount);
            Assert.Equal(300m, record.PrincipalPart);
            Assert.Equal(900m, loan.OutstandingPrincipal);
            Assert.True(loan.MonthlyPayment < paymentBefore);
            Assert.Equal(12, loan.Installments.Count(i => !i.IsCancelled));
            Assert.Equal(900m, loan.Installments.Sum(i => i.PrincipalAmount));
        }

        [Fact]
        public void ApplyPartialEarly_lowers_the_rate_when_enough_principal_is_paid()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            var record = LoanEngine.ApplyPartialEarly(loan, 300m, new DateTime(2026, 11, 10), "card", "LP-7", Bank());
            Assert.Equal(9m, loan.AnnualInterestRate);
            Assert.Equal(9m, record.InterestRateAfter);
        }

        [Fact]
        public void ApplyPartialEarly_keeps_the_rate_for_a_small_payment()
        {
            var loan = ActiveLoan(1200m, 10m, 12);
            var record = LoanEngine.ApplyPartialEarly(loan, 50m, new DateTime(2026, 11, 10), "card", "LP-8", Bank());
            Assert.Equal(10m, loan.AnnualInterestRate);
            Assert.Equal(10m, record.InterestRateAfter);
        }

        [Fact]
        public void ApplyPartialEarly_never_goes_below_the_minimum_rate()
        {
            var loan = ActiveLoan(1200m, 5m, 12);
            LoanEngine.ApplyPartialEarly(loan, 300m, new DateTime(2026, 11, 10), "card", "LP-9", Bank());
            Assert.Equal(5m, loan.AnnualInterestRate);
        }

        // ---------- Vaxtından əvvəl tam bağlama ----------

        [Fact]
        public void GetPayoff_without_interest_is_the_remaining_principal()
        {
            var loan = ActiveLoan(1200m, 0m, 12);
            Assert.Equal(1200m, LoanEngine.GetPayoff(loan, new DateTime(2026, 11, 10)));
        }

        [Fact]
        public void GetPayoff_includes_interest_only_for_the_days_that_have_passed_in_the_current_month()
        {
            var loan = ActiveLoan(1200m, 12m, 12);
            var nextInterest = loan.Installments.First(i => i.Number == 1).InterestAmount;      // 12
            var principal = loan.Installments.Sum(i => i.PrincipalAmount);

            var onDisbursement = LoanEngine.GetPayoff(loan, new DateTime(2026, 10, 15));
            var beforeFirstDue = LoanEngine.GetPayoff(loan, new DateTime(2026, 11, 29));

            Assert.Equal(principal, onDisbursement);
            Assert.True(beforeFirstDue > principal);
            Assert.True(beforeFirstDue <= principal + nextInterest);
        }

        [Fact]
        public void GetPayoff_on_a_due_date_includes_that_installment_in_full()
        {
            var loan = ActiveLoan(1200m, 12m, 12);
            var first = loan.Installments.First(i => i.Number == 1);
            var future = loan.Installments.Where(i => i.Number > 1).Sum(i => i.PrincipalAmount);
            var payoff = LoanEngine.GetPayoff(loan, first.DueDate);
            Assert.True(payoff >= first.PrincipalAmount + first.InterestAmount + future);
        }

        [Fact]
        public void ApplyPayoff_closes_the_loan_and_cancels_the_future_installments()
        {
            var loan = ActiveLoan(1200m, 0m, 12);
            var asOf = new DateTime(2026, 11, 10);
            var expected = LoanEngine.GetPayoff(loan, asOf);

            var records = LoanEngine.ApplyPayoff(loan, asOf, "card", "LP-10");

            Assert.Single(records);
            Assert.Equal(LoanPaymentType.FullEarly, records[0].Type);
            Assert.Equal(expected, records.Sum(r => r.Amount));
            Assert.All(loan.Installments, i => Assert.True(i.IsCancelled));
            Assert.True(loan.ClosedEarly);
            Assert.Equal(LoanStatus.Closed, loan.Status);
            Assert.Equal(0m, loan.OutstandingPrincipal);
        }

        [Fact]
        public void ApplyPayoff_amount_equals_GetPayoff_for_a_loan_with_interest_and_a_late_installment()
        {
            var loan = ActiveLoan(1200m, 12m, 12);
            var asOf = new DateTime(2026, 12, 8);                         // 1-ci ödəniş 8 gün gecikib
            var expected = LoanEngine.GetPayoff(loan, asOf);

            var records = LoanEngine.ApplyPayoff(loan, asOf, "card", "LP-11");

            Assert.Equal(expected, records.Sum(r => r.Amount));
            Assert.True(records.First().PenaltyPart > 0m);
            Assert.Equal(LoanStatus.Closed, loan.Status);
        }

        // ---------- Yeniləmə, məhkəmə, aktivləşdirmə ----------

        [Fact]
        public void Refresh_keeps_the_loan_open_while_something_is_unpaid()
        {
            var loan = ThreeInstallmentLoan();
            LoanEngine.Refresh(loan, new DateTime(2026, 10, 1));
            Assert.Equal(LoanStatus.Active, loan.Status);
            Assert.Equal(300m, loan.OutstandingPrincipal);
        }

        [Fact]
        public void Refresh_closes_a_loan_in_court_when_it_is_fully_paid()
        {
            var loan = ThreeInstallmentLoan();
            loan.Status = LoanStatus.Court;
            foreach (var installment in loan.Installments)
            {
                installment.PaidAmount = 110m;
            }
            LoanEngine.Refresh(loan, new DateTime(2026, 12, 1));
            Assert.Equal(LoanStatus.Closed, loan.Status);
            Assert.Equal(new DateTime(2026, 12, 1), loan.ClosedAt);
        }

        [Fact]
        public void Refresh_does_not_reopen_or_close_a_loan_that_was_not_active()
        {
            var loan = ThreeInstallmentLoan();
            loan.Status = LoanStatus.Rejected;
            foreach (var installment in loan.Installments)
            {
                installment.PaidAmount = 110m;
            }
            LoanEngine.Refresh(loan, new DateTime(2026, 12, 1));
            Assert.Equal(LoanStatus.Rejected, loan.Status);
        }

        [Fact]
        public void MoveToCourtIfNeeded_moves_after_the_configured_days()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 9, 30).AddDays(90).AddHours(-4);   // Bakı günü: dəqiq 90 gün gecikmə
            Assert.True(LoanEngine.MoveToCourtIfNeeded(loan, asOf, 90));
            Assert.Equal(LoanStatus.Court, loan.Status);
            Assert.Equal(asOf, loan.CourtAt);
        }

        [Fact]
        public void MoveToCourtIfNeeded_waits_until_the_limit_is_reached()
        {
            var loan = ThreeInstallmentLoan();
            var asOf = new DateTime(2026, 9, 30).AddDays(89);
            Assert.False(LoanEngine.MoveToCourtIfNeeded(loan, asOf, 90));
            Assert.Equal(LoanStatus.Active, loan.Status);
            Assert.Null(loan.CourtAt);
        }

        [Fact]
        public void MoveToCourtIfNeeded_ignores_loans_that_are_not_active_and_late_installments_that_are_paid()
        {
            var loan = ThreeInstallmentLoan();
            loan.Status = LoanStatus.Closed;
            Assert.False(LoanEngine.MoveToCourtIfNeeded(loan, new DateTime(2027, 6, 1), 90));

            var paid = ThreeInstallmentLoan();
            foreach (var installment in paid.Installments)
            {
                installment.PaidAmount = 110m;
            }
            Assert.False(LoanEngine.MoveToCourtIfNeeded(paid, new DateTime(2027, 6, 1), 90));
        }

        [Fact]
        public void Activate_builds_the_schedule_and_opens_the_loan()
        {
            var loan = ActiveLoan(1200m, 0m, 12);

            Assert.Equal(LoanStatus.Active, loan.Status);
            Assert.Equal(12, loan.Installments.Count);
            Assert.Equal(new DateTime(2026, 11, 30), loan.Installments.First().DueDate);
            Assert.Equal(new DateTime(2027, 10, 31), loan.Installments.Last().DueDate);
            Assert.Equal(100m, loan.MonthlyPayment);
            Assert.Equal(1200m, loan.TotalRepayment);
            Assert.Equal(1200m, loan.OutstandingPrincipal);
            Assert.Equal(new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc), loan.DisbursedAt);
        }

        [Fact]
        public void Activate_uses_the_approved_amount_when_it_is_less_than_requested()
        {
            var loan = new Loan { RequestedAmount = 2000m, ApprovedAmount = 1200m, TermMonths = 12, AnnualInterestRate = 0m };
            LoanEngine.Activate(loan, new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc));
            Assert.Equal(1200m, loan.OutstandingPrincipal);
            Assert.Equal(1200m, loan.Installments.Sum(i => i.PrincipalAmount));
        }

        [Fact]
        public void Activate_uses_the_requested_amount_when_nothing_was_approved_separately()
        {
            var loan = new Loan { RequestedAmount = 600m, ApprovedAmount = null, TermMonths = 6, AnnualInterestRate = 0m };
            LoanEngine.Activate(loan, new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc));
            Assert.Equal(600m, loan.OutstandingPrincipal);
        }

        [Fact]
        public void Activate_uses_the_baku_day_for_the_first_due_date()
        {
            // 31 oktyabr 21:00 UTC = 1 noyabr 01:00 Bakı: ilk ödəniş dekabrın sonunda
            var loan = new Loan { RequestedAmount = 600m, ApprovedAmount = 600m, TermMonths = 6, AnnualInterestRate = 0m };
            LoanEngine.Activate(loan, new DateTime(2026, 10, 31, 21, 0, 0, DateTimeKind.Utc));
            Assert.Equal(new DateTime(2026, 12, 31), loan.Installments.First().DueDate);
        }
    }
}
