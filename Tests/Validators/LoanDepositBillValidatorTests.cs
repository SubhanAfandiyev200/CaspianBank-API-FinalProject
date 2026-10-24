using Service.Helpers.DTOs.Bank;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.DTOs.Deposits;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Validators.Bank;
using Service.Helpers.Validators.Bills;
using Service.Helpers.Validators.Deposits;
using Service.Helpers.Validators.Loans;
using Xunit;

namespace CaspianBank.Tests.Validators
{
    public class LoanDepositBillValidatorTests
    {
        private static string NewId()
        {
            return Guid.NewGuid().ToString();
        }

        // ---------- ApplyLoan ----------

        private static ApplyLoanDto ValidApply()
        {
            return new ApplyLoanDto { Amount = 5000m, Purpose = "Consumer", TermMonths = 12, TargetCardId = 1 };
        }

        [Fact]
        public void ApplyLoan_accepts_a_normal_application()
        {
            Assert.True(V.IsValid(new ApplyLoanDtoValidator(), ValidApply()));
        }

        [Theory]
        [InlineData(0.01, true)]
        [InlineData(0, false)]
        [InlineData(-100, false)]
        [InlineData(100.555, false)]
        public void ApplyLoan_amount_is_positive_with_two_decimals(double amount, bool valid)
        {
            var dto = ValidApply();
            dto.Amount = (decimal)amount;
            Assert.Equal(valid, V.IsValid(new ApplyLoanDtoValidator(), dto));
        }

        [Theory]
        [InlineData("Consumer", true)]
        [InlineData("car", true)]
        [InlineData("MORTGAGE", true)]
        [InlineData("Medical", true)]
        [InlineData("Holiday", false)]
        [InlineData("", false)]
        public void ApplyLoan_purpose_must_be_a_known_value(string purpose, bool valid)
        {
            var dto = ValidApply();
            dto.Purpose = purpose;
            Assert.Equal(valid, V.IsValid(new ApplyLoanDtoValidator(), dto));
        }

        [Fact]
        public void ApplyLoan_other_purpose_needs_a_note_of_five_characters()
        {
            var validator = new ApplyLoanDtoValidator();
            var dto = ValidApply();
            dto.Purpose = "Other";

            dto.PurposeNote = null;
            Assert.False(V.IsValid(validator, dto));
            dto.PurposeNote = "  abc ";
            Assert.False(V.IsValid(validator, dto));
            dto.PurposeNote = "wedding";
            Assert.True(V.IsValid(validator, dto));
        }

        [Fact]
        public void ApplyLoan_purpose_note_is_at_most_200_characters()
        {
            var dto = ValidApply();
            dto.PurposeNote = V.Text(201);
            Assert.False(V.IsValid(new ApplyLoanDtoValidator(), dto));
            dto.PurposeNote = V.Text(200);
            Assert.True(V.IsValid(new ApplyLoanDtoValidator(), dto));
        }

        [Fact]
        public void ApplyLoan_needs_a_term_and_a_card()
        {
            var dto = ValidApply();
            dto.TermMonths = 0;
            dto.TargetCardId = 0;
            var errors = V.Errors(new ApplyLoanDtoValidator(), dto);
            Assert.Contains("Choose the term.", errors);
            Assert.Contains("Choose the card that receives the money.", errors);
        }

        // ---------- Approve / Reject ----------

        [Theory]
        [InlineData(1000, true)]
        [InlineData(0, false)]
        [InlineData(-5, false)]
        [InlineData(1000.123, false)]
        public void ApproveLoan_amount_is_positive_with_two_decimals(double amount, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new ApproveLoanDtoValidator(), new ApproveLoanDto { ApprovedAmount = (decimal)amount }));
        }

        [Theory]
        [InlineData("abc", true)]
        [InlineData("  abc  ", true)]
        [InlineData("ab", false)]
        [InlineData("   ", false)]
        [InlineData("", false)]
        public void RejectLoan_reason_is_at_least_three_characters(string reason, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new RejectLoanDtoValidator(), new RejectLoanDto { Reason = reason }));
        }

        [Fact]
        public void RejectLoan_reason_is_at_most_300_characters()
        {
            Assert.True(V.IsValid(new RejectLoanDtoValidator(), new RejectLoanDto { Reason = V.Text(300) }));
            Assert.False(V.IsValid(new RejectLoanDtoValidator(), new RejectLoanDto { Reason = V.Padded(301) }));
        }

        // ---------- LoanReviewFilter ----------

        [Fact]
        public void LoanReviewFilter_defaults_are_valid()
        {
            Assert.True(V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto()));
        }

        [Theory]
        [InlineData("Pending", true)]
        [InlineData("court", true)]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("Unknown", false)]
        public void LoanReviewFilter_status_is_empty_or_a_loan_status(string? status, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto { Status = status }));
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(1, 100, true)]
        [InlineData(0, 20, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 101, false)]
        public void LoanReviewFilter_paging_is_limited(int page, int pageSize, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto { Page = page, PageSize = pageSize }));
        }

        [Fact]
        public void LoanReviewFilter_search_is_at_most_60_characters()
        {
            Assert.True(V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto { Search = V.Text(60) }));
            Assert.False(V.IsValid(new LoanReviewFilterDtoValidator(), new LoanReviewFilterDto { Search = V.Text(61) }));
        }

        // ---------- PayLoan ----------

        [Fact]
        public void PayLoan_installment_without_an_amount_is_valid()
        {
            Assert.True(V.IsValid(new PayLoanDtoValidator(), new PayLoanDto { CardId = 1, Type = "Installment", Amount = 0m, RequestId = NewId() }));
        }

        [Fact]
        public void PayLoan_installment_part_amount_is_valid_and_negative_is_not()
        {
            var validator = new PayLoanDtoValidator();
            Assert.True(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Installment", Amount = 25.5m, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Installment", Amount = -1m, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Installment", Amount = 1.001m, RequestId = NewId() }));
        }

        [Fact]
        public void PayLoan_partial_early_needs_a_positive_amount()
        {
            var validator = new PayLoanDtoValidator();
            Assert.True(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "PartialEarly", Amount = 100m, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "PartialEarly", Amount = 0m, RequestId = NewId() }));
        }

        [Fact]
        public void PayLoan_full_early_needs_no_amount()
        {
            Assert.True(V.IsValid(new PayLoanDtoValidator(), new PayLoanDto { CardId = 1, Type = "FullEarly", Amount = 0m, RequestId = NewId() }));
        }

        [Fact]
        public void PayLoan_type_card_and_request_id_are_checked()
        {
            var validator = new PayLoanDtoValidator();
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Magic", RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 0, Type = "Installment", RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Installment", RequestId = "x" }));
            Assert.False(V.IsValid(validator, new PayLoanDto { CardId = 1, Type = "Installment", RequestId = null }));
        }

        // ---------- PayByCode ----------

        [Theory]
        [InlineData("KR-2026-000123", true)]
        [InlineData("KR-2026-00012", false)]
        [InlineData("kr-2026-000123", false)]
        [InlineData("KR-26-000123", false)]
        [InlineData("2026-000123", false)]
        [InlineData("", false)]
        public void PayByCode_contract_number_has_the_KR_format(string code, bool valid)
        {
            Assert.Equal(valid, V.IsValid(new PayByCodeDtoValidator(), new PayByCodeDto { ContractNumber = code, CardId = 1, Amount = 0m, RequestId = NewId() }));
        }

        [Fact]
        public void PayByCode_amount_is_empty_or_a_valid_part()
        {
            var validator = new PayByCodeDtoValidator();
            Assert.True(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 1, Amount = 0m, RequestId = NewId() }));
            Assert.True(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 1, Amount = 4m, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 1, Amount = -4m, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 1, Amount = 4.123m, RequestId = NewId() }));
        }

        [Fact]
        public void PayByCode_needs_a_card_and_a_request_id()
        {
            var validator = new PayByCodeDtoValidator();
            Assert.False(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 0, RequestId = NewId() }));
            Assert.False(V.IsValid(validator, new PayByCodeDto { ContractNumber = "KR-2026-000123", CardId = 1, RequestId = "bad" }));
        }

        // ---------- Deposit ----------

        private static OpenDepositDto ValidDeposit()
        {
            return new OpenDepositDto { PlanId = 1, Amount = 1000m, SourceCardId = 1, PayoutCardId = 2, RequestId = NewId() };
        }

        [Fact]
        public void OpenDeposit_accepts_a_normal_request()
        {
            Assert.True(V.IsValid(new OpenDepositDtoValidator(), ValidDeposit()));
        }

        [Fact]
        public void OpenDeposit_checks_every_field()
        {
            var validator = new OpenDepositDtoValidator();

            var dto = ValidDeposit();
            dto.PlanId = 0;
            Assert.Contains("Choose the deposit term.", V.Errors(validator, dto));

            dto = ValidDeposit();
            dto.Amount = 0m;
            Assert.Contains("Enter the amount you want to deposit.", V.Errors(validator, dto));

            dto = ValidDeposit();
            dto.Amount = 10.005m;
            Assert.Contains("Use at most two decimal places.", V.Errors(validator, dto));

            dto = ValidDeposit();
            dto.SourceCardId = 0;
            Assert.Contains("Choose the card to pay from.", V.Errors(validator, dto));

            dto = ValidDeposit();
            dto.PayoutCardId = 0;
            Assert.Contains("Choose the card that receives the money back.", V.Errors(validator, dto));

            dto = ValidDeposit();
            dto.RequestId = "x";
            Assert.Contains("Invalid request id.", V.Errors(validator, dto));
        }

        [Fact]
        public void UpdateDepositPlan_rate_and_amounts_are_limited()
        {
            var validator = new UpdateDepositPlanDtoValidator();
            Assert.True(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 8.5m, MinAmount = 100m, MaxAmount = 100000m, IsActive = true }));
            Assert.True(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 0.1m, MinAmount = 1m, MaxAmount = 1m }));
            Assert.True(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 30m, MinAmount = 1m, MaxAmount = 10000000m }));

            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 0.05m, MinAmount = 100m, MaxAmount = 1000m }));
            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 30.01m, MinAmount = 100m, MaxAmount = 1000m }));
            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 8.555m, MinAmount = 100m, MaxAmount = 1000m }));
            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 8m, MinAmount = 0.5m, MaxAmount = 1000m }));
            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 8m, MinAmount = 100m, MaxAmount = 50m }));
            Assert.False(V.IsValid(validator, new UpdateDepositPlanDto { AnnualRate = 8m, MinAmount = 100m, MaxAmount = 10000001m }));
        }

        // ---------- Bills ----------

        [Fact]
        public void LookupBill_needs_a_provider_and_a_short_identifier()
        {
            var validator = new LookupBillDtoValidator();
            Assert.True(V.IsValid(validator, new LookupBillDto { ProviderId = 1, Identifier = "0551234567" }));
            Assert.False(V.IsValid(validator, new LookupBillDto { ProviderId = 0, Identifier = "0551234567" }));
            Assert.False(V.IsValid(validator, new LookupBillDto { ProviderId = 1, Identifier = "" }));
            Assert.True(V.IsValid(validator, new LookupBillDto { ProviderId = 1, Identifier = V.Text(25) }));
            Assert.False(V.IsValid(validator, new LookupBillDto { ProviderId = 1, Identifier = V.Text(26) }));
        }

        private static PayBillDto ValidPay()
        {
            return new PayBillDto { ProviderId = 1, Identifier = "0551234567", Amount = 10m, CardId = 1, RequestId = NewId() };
        }

        [Fact]
        public void PayBill_accepts_a_normal_payment()
        {
            Assert.True(V.IsValid(new PayBillDtoValidator(), ValidPay()));
        }

        [Fact]
        public void PayBill_checks_the_fields()
        {
            var validator = new PayBillDtoValidator();

            var dto = ValidPay();
            dto.Amount = 0m;
            Assert.Contains("Enter an amount.", V.Errors(validator, dto));

            dto = ValidPay();
            dto.Amount = 1.234m;
            Assert.Contains("The amount can have at most 2 decimals.", V.Errors(validator, dto));

            dto = ValidPay();
            dto.CardId = 0;
            Assert.Contains("Choose the card to pay from.", V.Errors(validator, dto));

            dto = ValidPay();
            dto.ProviderId = 0;
            Assert.Contains("Choose a provider.", V.Errors(validator, dto));

            dto = ValidPay();
            dto.Identifier = "";
            Assert.Contains("Enter the number or code.", V.Errors(validator, dto));

            dto = ValidPay();
            dto.RequestId = "no";
            Assert.Contains("Invalid request id.", V.Errors(validator, dto));
        }

        [Fact]
        public void UpdateBillProvider_needs_a_name_of_at_most_60_characters()
        {
            var validator = new UpdateBillProviderDtoValidator();
            Assert.True(V.IsValid(validator, new UpdateBillProviderDto { Name = "Azercell" }));
            Assert.False(V.IsValid(validator, new UpdateBillProviderDto { Name = "  " }));
            Assert.False(V.IsValid(validator, new UpdateBillProviderDto { Name = null }));
            Assert.True(V.IsValid(validator, new UpdateBillProviderDto { Name = V.Text(60) }));
            Assert.False(V.IsValid(validator, new UpdateBillProviderDto { Name = V.Padded(61) }));
        }

        [Fact]
        public void CreateRecurringPayment_loans_need_no_amount_but_other_providers_do()
        {
            var validator = new CreateRecurringPaymentDtoValidator();

            Assert.True(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = null, Identifier = "KR-2026-000123", Amount = 0m, CardId = 1 }));
            Assert.True(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = 3, Identifier = "0551234567", Amount = 15m, CardId = 1 }));
            Assert.False(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = 3, Identifier = "0551234567", Amount = 0m, CardId = 1 }));
            Assert.False(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = 3, Identifier = "0551234567", Amount = 1.234m, CardId = 1 }));
            Assert.False(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = 3, Identifier = "", Amount = 5m, CardId = 1 }));
            Assert.False(V.IsValid(validator, new CreateRecurringPaymentDto { ProviderId = 3, Identifier = "0551234567", Amount = 5m, CardId = 0 }));
        }

        // ---------- Bank qaydaları ----------

        private static BankRulesDto ValidRules()
        {
            return new BankRulesDto
            {
                ReservePercent = 10m, ProfitTaxPercent = 20m, OperatingCostMonthly = 150m, MaxInterbankAmount = 10000m,
                MinLoanAmount = 500m, MaxLoanAmount = 50000m, AnnualInterestRate = 10m, FixedPenalty = 10m, DailyPenaltyPercent = 0.5m,
                MaxActiveLoans = 3, OfferValidDays = 7
            };
        }

        [Fact]
        public void BankRules_accept_the_default_values()
        {
            Assert.True(V.IsValid(new BankRulesDtoValidator(), ValidRules()));
        }

        [Fact]
        public void BankRules_reject_values_outside_their_ranges()
        {
            var validator = new BankRulesDtoValidator();
            var cases = new List<Action<BankRulesDto>>
            {
                d => d.ReservePercent = -1m,
                d => d.ReservePercent = 100.01m,
                d => d.ProfitTaxPercent = 101m,
                d => d.OperatingCostMonthly = -1m,
                d => d.OperatingCostMonthly = 100000001m,
                d => d.MaxInterbankAmount = 0.5m,
                d => d.MaxInterbankAmount = 1000001m,
                d => d.MinLoanAmount = 0m,
                d => d.MaxLoanAmount = 100m,                // minimumdan azdır
                d => d.MaxLoanAmount = 10000001m,
                d => d.AnnualInterestRate = 0.05m,
                d => d.AnnualInterestRate = 100.5m,
                d => d.FixedPenalty = -1m,
                d => d.FixedPenalty = 100001m,
                d => d.DailyPenaltyPercent = 101m,
                d => d.MaxActiveLoans = 0,
                d => d.MaxActiveLoans = 21,
                d => d.OfferValidDays = 0,
                d => d.OfferValidDays = 61
            };

            foreach (var change in cases)
            {
                var dto = ValidRules();
                change(dto);
                Assert.False(V.IsValid(validator, dto));
            }
        }

        [Fact]
        public void BankRules_allow_the_boundary_values()
        {
            var dto = new BankRulesDto
            {
                ReservePercent = 0m, ProfitTaxPercent = 100m, OperatingCostMonthly = 0m, MaxInterbankAmount = 1m,
                MinLoanAmount = 1m, MaxLoanAmount = 1m, AnnualInterestRate = 0.1m, FixedPenalty = 0m, DailyPenaltyPercent = 0m,
                MaxActiveLoans = 1, OfferValidDays = 1
            };
            Assert.True(V.IsValid(new BankRulesDtoValidator(), dto));
        }

        [Fact]
        public void BankRules_allow_at_most_two_decimal_places()
        {
            var validator = new BankRulesDtoValidator();
            var dto = ValidRules();
            dto.ReservePercent = 10.555m;
            Assert.Contains("Use at most two decimal places for the reserve.", V.Errors(validator, dto));

            dto = ValidRules();
            dto.AnnualInterestRate = 9.999m;
            Assert.Contains("Use at most two decimal places for the loan rate.", V.Errors(validator, dto));
        }
    }
}
