using FluentValidation;
using Service.Helpers.DTOs.Brands;

namespace Service.Helpers.Validators.Brands
{
    public class UpdateBrandDtoValidator : AbstractValidator<UpdateBrandDto>
    {
        public UpdateBrandDtoValidator()
        {
            RuleFor(x => x.Name).RequiredText("name", 100);

            // Şəkil istəyə bağlıdır (boş olarsa köhnə qalır). Verilibsə ölçüsünü və növünü FileService yoxlayır
        }
    }
}
