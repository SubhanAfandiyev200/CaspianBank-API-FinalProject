using FluentValidation;
using Service.Helpers.DTOs.Bank;

namespace Service.Helpers.Validators.Bank
{
    public class BankRulesDtoValidator : AbstractValidator<BankRulesDto>
    {
        public BankRulesDtoValidator()
        {
            RuleFor(x => x.ReservePercent).InclusiveBetween(0m, 100m).WithMessage("The reserve must be between 0% and 100%.").Must(Two).WithMessage("Use at most two decimal places for the reserve.");
            RuleFor(x => x.ProfitTaxPercent).InclusiveBetween(0m, 100m).WithMessage("The profit tax must be between 0% and 100%.").Must(Two).WithMessage("Use at most two decimal places for the tax.");
            RuleFor(x => x.OperatingCostMonthly).InclusiveBetween(0m, 100000000m).WithMessage("The monthly operating cost must be between 0 and 100,000,000 AZN.").Must(Two).WithMessage("Use at most two decimal places for the operating cost.");
            RuleFor(x => x.MaxInterbankAmount).InclusiveBetween(1m, 1000000m).WithMessage("The most for one transfer to another bank must be between 1 and 1,000,000 AZN.").Must(Two).WithMessage("Use at most two decimal places for the interbank limit.");

            RuleFor(x => x.MinLoanAmount).InclusiveBetween(1m, 10000000m).WithMessage("The smallest loan must be between 1 and 10,000,000 AZN.").Must(Two).WithMessage("Use at most two decimal places for the smallest loan.");
            RuleFor(x => x.MaxLoanAmount).GreaterThanOrEqualTo(x => x.MinLoanAmount).WithMessage("The largest loan can't be smaller than the smallest.").LessThanOrEqualTo(10000000m).WithMessage("The largest loan is too big.").Must(Two).WithMessage("Use at most two decimal places for the largest loan.");
            RuleFor(x => x.AnnualInterestRate).InclusiveBetween(0.1m, 100m).WithMessage("The loan rate must be between 0.1% and 100%.").Must(Two).WithMessage("Use at most two decimal places for the loan rate.");
            RuleFor(x => x.FixedPenalty).InclusiveBetween(0m, 100000m).WithMessage("The fixed penalty must be between 0 and 100,000 AZN.").Must(Two).WithMessage("Use at most two decimal places for the fixed penalty.");
            RuleFor(x => x.DailyPenaltyPercent).InclusiveBetween(0m, 100m).WithMessage("The daily penalty must be between 0% and 100%.").Must(Two).WithMessage("Use at most two decimal places for the daily penalty.");
            RuleFor(x => x.MaxActiveLoans).InclusiveBetween(1, 20).WithMessage("The number of open loans must be between 1 and 20.");
            RuleFor(x => x.OfferValidDays).InclusiveBetween(1, 60).WithMessage("An offer must stay valid for 1 to 60 days.");
        }

        private static bool Two(decimal value)
        {
            return decimal.Round(value, 2) == value;
        }
    }
}
