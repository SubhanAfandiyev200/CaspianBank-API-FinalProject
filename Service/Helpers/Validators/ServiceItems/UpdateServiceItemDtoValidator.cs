using FluentValidation;
using Service.Helpers.DTOs.ServiceItems;

namespace Service.Helpers.Validators.ServiceItems
{
    public class UpdateServiceItemDtoValidator : AbstractValidator<ServiceItemUpdateDto>
    {
        // 100 / 300 = bazadakı ServiceItems sütunlarının uzunluğu (ServiceItemConfiguration)
        public UpdateServiceItemDtoValidator()
        {
            RuleFor(x => x.Title).RequiredText("title", 100);
            RuleFor(x => x.Description).RequiredText("description", 300);
        }
    }
}
