using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    public class RejectLoanDtoValidator : AbstractValidator<RejectLoanDto>
    {
        public RejectLoanDtoValidator()
        {
            RuleFor(x => x.Reason)
                .Must(r => !string.IsNullOrWhiteSpace(r) && r.Trim().Length >= 3)
                    .WithMessage("Write the reason for declining (at least 3 characters).")
                .Must(r => string.IsNullOrWhiteSpace(r) || r.Trim().Length <= 300)
                    .WithMessage("The reason can be at most 300 characters.");
        }
    }
}
