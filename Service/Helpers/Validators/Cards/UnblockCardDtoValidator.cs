using FluentValidation;
using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.Validators.Cards
{
    public class UnblockCardDtoValidator : AbstractValidator<UnblockCardDto>
    {
        public UnblockCardDtoValidator()
        {
            RuleFor(x => x.Code)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Enter the 6-digit code.")
                .Matches(@"^\d{6}$").WithMessage("The code is 6 digits.");
        }
    }
}
