using FluentValidation;
using Service.Helpers.DTOs.Settings;

namespace Service.Helpers.Validators.Settings
{
    public class UpdateSettingLogoDtoValidator : AbstractValidator<UpdateSettingLogoDto>
    {
        public UpdateSettingLogoDtoValidator()
        {
            // Şəklin ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Image)
                .Must(image => image is not null && image.Length > 0).WithMessage("Choose an image.");
        }
    }
}
