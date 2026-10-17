using FluentValidation;
using Service.Helpers.DTOs.History;

namespace Service.Helpers.Validators.History
{
    public class StatementFilterDtoValidator : AbstractValidator<StatementFilterDto>
    {
        // Çıxarışın ən uzun aralığı (gün)
        private const int MaxDays = 366;

        public StatementFilterDtoValidator()
        {
            RuleFor(x => x.CardId)
                .NotNull().WithMessage("Choose a card.");

            RuleFor(x => x.From)
                .NotNull().WithMessage("Choose the start date.");

            RuleFor(x => x.To)
                .NotNull().WithMessage("Choose the end date.");

            RuleFor(x => x)
                .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value.Date <= x.To.Value.Date)
                .WithMessage("The start date must not be after the end date.");

            RuleFor(x => x)
                .Must(x => !x.From.HasValue || !x.To.HasValue || (x.To.Value.Date - x.From.Value.Date).TotalDays < MaxDays)
                .WithMessage("The statement can cover at most one year.");
        }
    }
}
