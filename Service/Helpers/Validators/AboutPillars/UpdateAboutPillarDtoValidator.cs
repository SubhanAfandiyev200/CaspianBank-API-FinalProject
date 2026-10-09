using FluentValidation;
using Service.Helpers.DTOs.AboutPillars;

namespace Service.Helpers.Validators.AboutPillars
{
    public class UpdateAboutPillarDtoValidator : AbstractValidator<UpdateAboutPillarDto>
    {
        public UpdateAboutPillarDtoValidator()
        {
            RuleFor(x => x.Title).RequiredText("title", 100);
            RuleFor(x => x.Description).RequiredText("description", 300);

            // Şəkil istəyə bağlıdır (boş olarsa köhnə qalır). Verilibsə ölçüsünü və növünü FileService yoxlayır
        }
    }
}
