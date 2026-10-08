using FluentValidation;
using Service.Helpers.DTOs.CardDesigns;

namespace Service.Helpers.Validators.CardDesigns
{
    public class CreateCardDesignDtoValidator : AbstractValidator<CreateCardDesignDto>
    {
        public CreateCardDesignDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Enter a title.")
                .MaximumLength(50).WithMessage("The title can be at most 50 characters.");

            RuleFor(x => x.DisplayOrder)
                .InclusiveBetween(0, 1000).WithMessage("The order must be between 0 and 1000.");

            // Şəklin ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Image)
                .NotNull().WithMessage("Choose a card image.")
                .Must(i => i is null || i.Length > 0).WithMessage("Choose a card image.");
        }
    }
}
