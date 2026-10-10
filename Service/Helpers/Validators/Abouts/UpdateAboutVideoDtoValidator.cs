using FluentValidation;
using Service.Helpers.DTOs.Abouts;

namespace Service.Helpers.Validators.Abouts
{
    public class UpdateAboutVideoDtoValidator : AbstractValidator<UpdateAboutVideoDto>
    {
        public UpdateAboutVideoDtoValidator()
        {
            // Videonun ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Video)
                .Must(video => video is not null && video.Length > 0).WithMessage("Choose a video.");
        }
    }
}
