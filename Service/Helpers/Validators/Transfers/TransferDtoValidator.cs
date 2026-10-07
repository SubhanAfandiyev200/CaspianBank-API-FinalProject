using FluentValidation;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Validators.Cards;

namespace Service.Helpers.Validators.Transfers
{
    public class TransferDtoValidator : AbstractValidator<TransferDto>
    {
        public const decimal MaxAmount = 100000m;

        public TransferDtoValidator()
        {
            RuleFor(x => x.FromCardId)
                .GreaterThan(0).WithMessage("Choose a card to send from.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Enter an amount greater than zero.")
                .LessThanOrEqualTo(MaxAmount).WithMessage("A single transfer can be at most 100,000 AZN.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");

            // Alıcı: ya öz kart, ya başqa kartın nömrəsi (ikisi birlikdə yox)
            RuleFor(x => x)
                .Must(x => x.ToCardId.HasValue ^ !string.IsNullOrWhiteSpace(x.ToCardNumber))
                .WithMessage("Choose one of your cards or enter the recipient's card number.")
                .OverridePropertyName("Recipient");

            When(x => !string.IsNullOrWhiteSpace(x.ToCardNumber), () =>
            {
                RuleFor(x => x.ToCardNumber!)
                    .Must(n => TopUpDtoValidator.Digits(n).Length == 16)
                    .WithMessage("Enter a 16-digit card number.");
            });

            RuleFor(x => x.Note)
                .MaximumLength(140).WithMessage("The note can be at most 140 characters.");

            RuleFor(x => x.RequestId)
                .Must(id => string.IsNullOrEmpty(id) || Guid.TryParse(id, out _))
                .WithMessage("Invalid request id.");
        }
    }
}
