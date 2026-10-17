using FluentValidation;
using Service.Helpers.DTOs.Notifications;

namespace Service.Helpers.Validators.Notifications
{
    public class NotificationFilterDtoValidator : AbstractValidator<NotificationFilterDto>
    {
        public NotificationFilterDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("The page must be 1 or more.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50).WithMessage("The page size must be between 1 and 50.");
        }
    }
}
