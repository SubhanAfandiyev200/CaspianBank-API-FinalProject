using FluentValidation;
using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.Validators.Cards
{
    public class BlockCardDtoValidator : AbstractValidator<BlockCardDto>
    {
        public BlockCardDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Enter the email on this account.")
                .EmailAddress().WithMessage("Enter a valid email address.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Enter your password.");
        }
    }
}
