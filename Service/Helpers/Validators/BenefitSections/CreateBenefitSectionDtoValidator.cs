using FluentValidation;
using Service.Helpers.DTOs.BenefitSections;

namespace Service.Helpers.Validators.BenefitSections
{
    public class CreateBenefitSectionDtoValidator : AbstractValidator<BenefitSectionCreateDto>
    {
        public CreateBenefitSectionDtoValidator()
        {
            // Limitlər bazadakı uzunluqlardır (BenefitSectionConfiguration)
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 500);
        }
    }
}
