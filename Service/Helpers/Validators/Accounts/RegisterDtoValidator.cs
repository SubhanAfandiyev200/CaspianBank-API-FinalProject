using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.")
                .MaximumLength(256);

            RuleFor(x => x.Fin)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter the 7-character FIN code.")
                .Matches("^[A-Za-z0-9]{7}$").WithMessage("The FIN code must be 7 letters or digits.");

            RuleFor(x => x.IdCardNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter your ID card number.")
                .Matches("^(?i)AZE[0-9]{8}$").WithMessage("The ID card number looks like AZE12345678.");

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

            RuleFor(x => x.VerificationToken)
                .NotEmpty().WithMessage("Email verification is required.");

            RuleFor(x => x.Password).MustBeStrongPassword();
        }
    }
}
