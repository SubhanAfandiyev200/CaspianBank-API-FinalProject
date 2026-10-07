using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        private const int MinAge = 18;

        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.")
                .MaximumLength(256);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Enter your first name.")
                .MaximumLength(100);

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Enter your last name.")
                .MaximumLength(100);

            // Rəqəmlər boşluqsuz gəlir: 0 ilə 10 rəqəm (0777610606) və ya 0-suz 9 rəqəm (777610606)
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Enter your phone number.")
                .Matches(@"^(0\d{9}|[1-9]\d{8})$").WithMessage("Enter a valid Azerbaijan mobile number.");

            RuleFor(x => x.BirthDay)
                .NotEmpty().WithMessage("Enter your date of birth.")
                .Must(BeAdult).WithMessage($"You must be {MinAge} or older to open an account.");

            RuleFor(x => x.VerificationToken)
                .NotEmpty().WithMessage("Email verification is required.");

            RuleFor(x => x.Password).MustBeStrongPassword();
        }

        private static bool BeAdult(DateTime birthDay)
        {
            var today = DateTime.UtcNow.Date;
            if (birthDay.Date > today)
            {
                return false;
            }
            var age = today.Year - birthDay.Year;
            if (birthDay.Date > today.AddYears(-age))
            {
                age--;
            }
            return age >= MinAge;
        }
    }
}
