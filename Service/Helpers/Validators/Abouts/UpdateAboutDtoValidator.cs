using FluentValidation;
using Service.Helpers.DTOs.Abouts;

namespace Service.Helpers.Validators.Abouts
{
    public class UpdateAboutDtoValidator : AbstractValidator<AboutUpdateDto>
    {
        // 100 / 200 / 500 = bazadakı Abouts sütunlarının uzunluğu (AboutConfiguration)
        public UpdateAboutDtoValidator()
        {
            RuleFor(x => x.Label).RequiredText("label", 100);
            RuleFor(x => x.Title).RequiredText("title", 200);
            RuleFor(x => x.Description).RequiredText("description", 500);
        }
    }
}
