using FluentValidation;
using Service.Helpers.DTOs.HomeTickers;

namespace Service.Helpers.Validators.HomeTickers
{
    public class CreateHomeTickerDtoValidator : AbstractValidator<CreateHomeTickerDto>
    {
        public CreateHomeTickerDtoValidator()
        {
            // 100 = bazadakı HomeTickers.Text uzunluğu (HomeTickerConfiguration)
            RuleFor(x => x.Text).RequiredText("text", 100);
        }
    }
}
