using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.History;

namespace Service.Helpers.Validators.History
{
    public class HistoryFilterDtoValidator : AbstractValidator<HistoryFilterDto>
    {
        public HistoryFilterDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("The page must be 1 or more.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50).WithMessage("The page size must be between 1 and 50.");

            RuleFor(x => x.Type)
                .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<TransactionType>(value, true, out _))
                .WithMessage("Choose a valid transaction type.");

            RuleFor(x => x.Direction)
                .Must(value => string.IsNullOrWhiteSpace(value)
                            || value.Equals("in", StringComparison.OrdinalIgnoreCase)
                            || value.Equals("out", StringComparison.OrdinalIgnoreCase))
                .WithMessage("The direction must be 'in' or 'out'.");

            RuleFor(x => x.Search)
                .Must(value => value is null || value.Trim().Length <= 50)
                .WithMessage("The search text can be at most 50 characters.");

            RuleFor(x => x)
                .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value.Date <= x.To.Value.Date)
                .WithMessage("The start date must not be after the end date.");
        }
    }
}
