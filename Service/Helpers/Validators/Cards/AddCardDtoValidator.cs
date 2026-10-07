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
                .Must(t => Enum.TryParse<CardTier>(t, true, out var tier) && Enum.IsDefined(tier) && tier != CardTier.Cashback)
                .WithMessage("Choose Standard, Silver or Gold.");

            RuleFor(x => x.FundingCardId)
                .GreaterThan(0).WithMessage("Choose a card to charge.");
        }
    }
}
