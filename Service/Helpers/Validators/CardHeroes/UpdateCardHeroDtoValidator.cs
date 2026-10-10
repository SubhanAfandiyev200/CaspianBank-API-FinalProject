using FluentValidation;
using Service.Helpers.DTOs.CardHeroes;

namespace Service.Helpers.Validators.CardHeroes
{
    public class UpdateCardHeroDtoValidator : AbstractValidator<UpdateCardHeroDto>
    {
        public UpdateCardHeroDtoValidator()
        {
            // Limitlər bazadakı uzunluqlardır (CardHeroConfiguration)
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 500);
        }
    }
}
