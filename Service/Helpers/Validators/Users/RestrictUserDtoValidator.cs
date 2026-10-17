using FluentValidation;
using Service.Helpers.DTOs.Users;

namespace Service.Helpers.Validators.Users
{
    public class RestrictUserDtoValidator : AbstractValidator<RestrictUserDto>
    {
        public RestrictUserDtoValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Write the reason for restricting the account.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 5)
                    .WithMessage("The reason must be at least 5 characters.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 300)
                    .WithMessage("The reason can be at most 300 characters.");
        }
    }
}
