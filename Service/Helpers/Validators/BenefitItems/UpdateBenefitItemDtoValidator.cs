using FluentValidation;
using Service.Helpers.DTOs.BenefitItems;

namespace Service.Helpers.Validators.BenefitItems
{
    public class UpdateBenefitItemDtoValidator : AbstractValidator<BenefitItemUpdateDto>
    {
        public UpdateBenefitItemDtoValidator()
        {
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 300);
            RuleFor(x => x.ButtonText).RequiredText("button text", 100);
            RuleFor(x => x.ButtonUrl).SitePath(300);
            RuleFor(x => x.Text1).RequiredText("first line", 200);
            RuleFor(x => x.Text2).RequiredText("second line", 200);
            RuleFor(x => x.Text3).RequiredText("third line", 200);
            RuleFor(x => x.BenefitSectionId).GreaterThan(0).WithMessage("Choose a section.");
        }
    }
}
