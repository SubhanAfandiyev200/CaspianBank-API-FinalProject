using FluentValidation;
using Service.Helpers.DTOs.Bills;

namespace Service.Helpers.Validators.Bills
{
    public class CreateRecurringPaymentDtoValidator : AbstractValidator<CreateRecurringPaymentDto>
    {
        public CreateRecurringPaymentDtoValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("Enter the number or code.")
                .MaximumLength(25).WithMessage("That number or code is too long.");

            // Kreditdə məbləğ yoxdur (hər ay həmin ayın aylıq ödənişi ödənilir), qalan təchizatçılarda məbləğ lazımdır
            When(x => x.ProviderId.HasValue && x.ProviderId.Value > 0, () =>
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0).WithMessage("Enter an amount.")
                    .Must(a => decimal.Round(a, 2) == a).WithMessage("The amount can have at most 2 decimals.");
            });

            RuleFor(x => x.CardId)
                .GreaterThan(0).WithMessage("Choose the card to pay from.");
        }
    }
}
