using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class CheckEmailDtoValidator : AbstractValidator<CheckEmailDto>
    {
        public CheckEmailDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter your email.")
                .EmailAddress().WithMessage("Enter a valid email address.")
                .MaximumLength(256);
        }
    }
}
