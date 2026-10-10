using FluentValidation;
using Service.Helpers.DTOs.HomeTickers;

namespace Service.Helpers.Validators.HomeTickers
{
    public class UpdateHomeTickerDtoValidator : AbstractValidator<UpdateHomeTickerDto>
    {
        public UpdateHomeTickerDtoValidator()
        {
            RuleFor(x => x.Text).RequiredText("text", 100);
        }
    }
}
