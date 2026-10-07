using FluentValidation;
using Service.Helpers.DTOs.Cards;
using System.Globalization;

namespace Service.Helpers.Validators.Cards
{
    public class TopUpDtoValidator : AbstractValidator<TopUpDto>
    {
        public const decimal MinAmount = 1m;
        public const decimal MaxAmount = 5000m;

        public TopUpDtoValidator()
        {
            RuleFor(x => x.Amount)
                .InclusiveBetween(MinAmount, MaxAmount).WithMessage("The amount must be between 1 and 5,000 AZN.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");

            RuleFor(x => x.SourceCardNumber)
                .NotEmpty().WithMessage("Enter the card number.")
                .Must(n => CardNumberGenerator.IsValid(Digits(n))).WithMessage("Enter a valid card number.");

            RuleFor(x => x.Expiry)
                .NotEmpty().WithMessage("Enter the expiry date.")
                .Must(BeFutureExpiry).WithMessage("Enter a valid expiry date (MM/YY) that has not passed.");

            RuleFor(x => x.Cvv)
                .NotEmpty().WithMessage("Enter the CVV.")
                .Matches("^[0-9]{3,4}$").WithMessage("The CVV must be 3 or 4 digits.");
        }

        // Boşluq və tireləri atır: "4111 1111 1111 1111" də qəbul olunur
        public static string Digits(string value)
        {
            return new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        }

        private static bool BeFutureExpiry(string value)
        {
            if (!DateTime.TryParseExact((value ?? string.Empty).Trim(), "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return false;
            }

            // Kartın bitmə ayı sonuna qədər etibarlıdır
            var lastDay = new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
            return lastDay >= DateTime.UtcNow.Date;
        }
    }
}
