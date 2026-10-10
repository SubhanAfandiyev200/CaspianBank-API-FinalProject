using FluentValidation;
using Service.Helpers.DTOs.BenefitSections;

namespace Service.Helpers.Validators.BenefitSections
{
    public class UpdateBenefitSectionDtoValidator : AbstractValidator<BenefitSectionUpdateDto>
    {
        public UpdateBenefitSectionDtoValidator()
        {
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 500);
        }
    }
}
