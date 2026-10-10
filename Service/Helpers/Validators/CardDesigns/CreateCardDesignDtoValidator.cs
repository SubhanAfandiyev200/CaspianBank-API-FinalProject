using FluentValidation;
using Service.Helpers.DTOs.CardDesigns;

namespace Service.Helpers.Validators.CardDesigns
{
    public class CreateCardDesignDtoValidator : AbstractValidator<CreateCardDesignDto>
    {
        public CreateCardDesignDtoValidator()
        {
            // 50 = bazadakı CardDesigns.Title uzunluğu (CardDesignConfiguration)
            RuleFor(x => x.Title).RequiredText("title", 50);

            RuleFor(x => x.DisplayOrder)
                .InclusiveBetween(0, 1000).WithMessage("The order must be between 0 and 1000.");

            // Şəklin ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Image)
                .Must(image => image is not null && image.Length > 0).WithMessage("Choose a card image.");
        }
    }
}
