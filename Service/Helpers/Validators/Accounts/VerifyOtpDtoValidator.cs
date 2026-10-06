using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class VerifyOtpDtoValidator : AbstractValidator<VerifyOtpDto>
    {
        public VerifyOtpDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Enter the 6-digit code.")
                .Matches(@"^\d{6}$").WithMessage("The code must be 6 digits.");
        }
    }
}
