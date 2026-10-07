using FluentValidation;
using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.Validators.Cards
{
    public class CreateInitialCardsDtoValidator : AbstractValidator<CreateInitialCardsDto>
    {
        public CreateInitialCardsDtoValidator()
        {
            RuleFor(x => x.Fin)
                .NotEmpty().WithMessage("Enter the 7-character FIN code.")
                .Matches("^[A-Za-z0-9]{7}$").WithMessage("The FIN code must be 7 letters or digits.");
        }
    }
}
