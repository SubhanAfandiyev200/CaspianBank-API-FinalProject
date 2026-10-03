using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("This reset link is invalid or has expired.")
                .EmailAddress().WithMessage("This reset link is invalid or has expired.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("This reset link is invalid or has expired.");

            RuleFor(x => x.NewPassword).MustBeStrongPassword();
        }
    }
}
