using FluentValidation;
using Service.Helpers.DTOs.Brands;

namespace Service.Helpers.Validators.Brands
{
    public class CreateBrandDtoValidator : AbstractValidator<CreateBrandDto>
    {
        public CreateBrandDtoValidator()
        {
            // 100 = bazadakı Brands.Name uzunluğu (BrandConfiguration)
            RuleFor(x => x.Name).RequiredText("name", 100);

            // Şəklin ölçüsünü və növünü FileService yoxlayır, burada yalnız seçilib-seçilmədiyi
            RuleFor(x => x.Image)
                .Must(image => image is not null && image.Length > 0).WithMessage("Choose an image.");
        }
    }
}
