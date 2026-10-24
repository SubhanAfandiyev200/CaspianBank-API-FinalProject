using FluentValidation;
using Service.Helpers.DTOs.Deposits;

namespace Service.Helpers.Validators.Deposits
{
    public class OpenDepositDtoValidator : AbstractValidator<OpenDepositDto>
    {
        public OpenDepositDtoValidator()
        {
            RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Choose the deposit term.");
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Enter the amount you want to deposit.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");
            RuleFor(x => x.SourceCardId).GreaterThan(0).WithMessage("Choose the card to pay from.");
            RuleFor(x => x.PayoutCardId).GreaterThan(0).WithMessage("Choose the card that receives the money back.");
            RuleFor(x => x.RequestId).Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid request id.");
        }
    }

    public class UpdateDepositPlanDtoValidator : AbstractValidator<UpdateDepositPlanDto>
    {
        public UpdateDepositPlanDtoValidator()
        {
            RuleFor(x => x.AnnualRate)
                .InclusiveBetween(0.1m, 30m).WithMessage("The yearly rate must be between 0.1% and 30%.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places for the rate.");
            RuleFor(x => x.MinAmount)
                .GreaterThanOrEqualTo(1m).WithMessage("The smallest amount must be at least 1 AZN.");
            RuleFor(x => x.MaxAmount)
                .GreaterThanOrEqualTo(x => x.MinAmount).WithMessage("The largest amount can't be smaller than the smallest.")
                .LessThanOrEqualTo(10000000m).WithMessage("The largest amount is too big.");
        }
    }
}
