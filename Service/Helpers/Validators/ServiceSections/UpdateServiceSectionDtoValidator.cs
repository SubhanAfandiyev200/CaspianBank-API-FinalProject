using FluentValidation;
using Service.Helpers.DTOs.ServiceSections;

namespace Service.Helpers.Validators.ServiceSections
{
    public class UpdateServiceSectionDtoValidator : AbstractValidator<ServiceSectionUpdateDto>
    {
        // 100 / 200 / 500 = bazadakı ServiceSections sütunlarının uzunluğu (ServiceSectionConfiguration)
        public UpdateServiceSectionDtoValidator()
        {
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 500);
        }
    }
}
