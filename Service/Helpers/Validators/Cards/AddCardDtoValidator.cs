using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.Validators.Cards
{
    public class AddCardDtoValidator : AbstractValidator<AddCardDto>
    {
        public AddCardDtoValidator()
        {
            // Cashback kartı yalnız ilk kartlarla birlikdə açılır, ayrıca istənə bilməz
            RuleFor(x => x.Tier)
                .Must(t => EnumRules.IsName<CardTier>(t) && !string.Equals(t.Trim(), nameof(CardTier.Cashback), StringComparison.OrdinalIgnoreCase))
                .WithMessage("Choose Standard, Silver or Gold.");

            RuleFor(x => x.FundingCardId)
                .GreaterThan(0).WithMessage("Choose a card to charge.");
        }
    }
}
