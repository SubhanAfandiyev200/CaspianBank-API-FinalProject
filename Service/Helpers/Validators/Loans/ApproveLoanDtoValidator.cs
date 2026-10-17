using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    // Təsdiq olunan məbləğin istənilən məbləğdən çox olmaması və bankın həddləri servisdə yoxlanılır
    public class ApproveLoanDtoValidator : AbstractValidator<ApproveLoanDto>
    {
        public ApproveLoanDtoValidator()
        {
            RuleFor(x => x.ApprovedAmount)
                .GreaterThan(0).WithMessage("Enter the amount to approve.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");
        }
    }
}
