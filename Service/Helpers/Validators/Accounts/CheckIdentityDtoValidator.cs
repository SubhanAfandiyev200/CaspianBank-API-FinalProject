using FluentValidation;
using Service.Helpers.DTOs.Accounts;

namespace Service.Helpers.Validators.Accounts
{
    public class CheckIdentityDtoValidator : AbstractValidator<CheckIdentityDto>
    {
        public CheckIdentityDtoValidator()
        {
            RuleFor(x => x.Fin)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter the 7-character FIN code.")
                .Matches("^[A-Za-z0-9]{7}$").WithMessage("The FIN code must be 7 letters or digits.");

            RuleFor(x => x.IdCardNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter your ID card number.")
                .Matches("^(?i)AZE[0-9]{8}$").WithMessage("The ID card number looks like AZE12345678.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter your first name.")
                .MaximumLength(100);

            RuleFor(x => x.Surname)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter your last name.")
                .MaximumLength(100);
        }
    }
}
