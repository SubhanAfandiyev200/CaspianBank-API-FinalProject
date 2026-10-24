using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    public class LoanReviewFilterDtoValidator : AbstractValidator<LoanReviewFilterDto>
    {
        public LoanReviewFilterDtoValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrWhiteSpace(s) || EnumRules.IsName<LoanStatus>(s))
                .WithMessage("Unknown status.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("The page number must be 1 or more.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("The page size must be between 1 and 100.");

            RuleFor(x => x.Search)
                .Must(s => string.IsNullOrWhiteSpace(s) || s.Trim().Length <= 60)
                .WithMessage("The search text can be at most 60 characters.");
        }
    }
}
