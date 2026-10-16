using FluentValidation;
using Service.Helpers.DTOs.Bills;

namespace Service.Helpers.Validators.Bills
{
    public class LookupBillDtoValidator : AbstractValidator<LookupBillDto>
    {
        public LookupBillDtoValidator()
        {
            RuleFor(x => x.ProviderId)
                .GreaterThan(0).WithMessage("Choose a provider.");

            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Enter the number or code.")
                .MaximumLength(25).WithMessage("That number or code is too long.");
        }
    }

    public class PayBillDtoValidator : AbstractValidator<PayBillDto>
    {
        public PayBillDtoValidator()
        {
            RuleFor(x => x.ProviderId)
                .GreaterThan(0).WithMessage("Choose a provider.");

            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Enter the number or code.")
                .MaximumLength(25).WithMessage("That number or code is too long.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Enter an amount.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("The amount can have at most 2 decimals.");

            RuleFor(x => x.CardId)
                .GreaterThan(0).WithMessage("Choose the card to pay from.");

            RuleFor(x => x.RequestId)
                .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid request id.");
        }
    }

    public class UpdateBillProviderDtoValidator : AbstractValidator<UpdateBillProviderDto>
    {
        public UpdateBillProviderDtoValidator()
        {
            RuleFor(x => x.Name).RequiredText("name", 60);
        }
    }
}
