using FluentValidation;
using Service.Helpers.DTOs.CardDesigns;

namespace Service.Helpers.Validators.CardDesigns
{
    public class UpdateCardDesignDtoValidator : AbstractValidator<UpdateCardDesignDto>
    {
        public UpdateCardDesignDtoValidator()
        {
            RuleFor(x => x.Title).RequiredText("title", 50);

            RuleFor(x => x.DisplayOrder)
                .InclusiveBetween(0, 1000).WithMessage("The order must be between 0 and 1000.");

            // Şəkil istəyə bağlıdır (boş olarsa köhnə qalır). Verilibsə ölçüsünü və növünü FileService yoxlayır
        }
    }
}
