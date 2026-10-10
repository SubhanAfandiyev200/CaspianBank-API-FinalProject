using FluentValidation;
using Service.Helpers.DTOs.AboutPillars;

namespace Service.Helpers.Validators.AboutPillars
{
    public class CreateAboutPillarDtoValidator : AbstractValidator<CreateAboutPillarDto>
    {
        // 100 / 300 = bazadakı AboutPillars sütunlarının uzunluğu (AboutPillarConfiguration)
        public CreateAboutPillarDtoValidator()
        {
            RuleFor(x => x.Title).RequiredText("title", 100);
            RuleFor(x => x.Description).RequiredText("description", 300);

            // Şəklin ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Image)
                .Must(image => image is not null && image.Length > 0).WithMessage("Choose an image.");
        }
    }
}
