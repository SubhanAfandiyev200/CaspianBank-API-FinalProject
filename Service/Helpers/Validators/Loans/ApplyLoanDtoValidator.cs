using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    // Şəkil və forma səviyyəsində yoxlama. Məbləğin bankın min/max həddinə uyğunluğu və müddət siyahısı servisdədir (OwnBank-dan oxunur)
    public class ApplyLoanDtoValidator : AbstractValidator<ApplyLoanDto>
    {
        public ApplyLoanDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Enter the amount you need.")
                .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");

            RuleFor(x => x.Purpose)
                .Must(p => Enum.TryParse<LoanPurpose>(p, true, out _)).WithMessage("Choose the purpose of the loan.");

            // "Other" seçiləndə səbəb yazılmalıdır
            When(x => string.Equals(x.Purpose, nameof(LoanPurpose.Other), StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.PurposeNote)
                    .Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 5)
                    .WithMessage("Describe the purpose (at least 5 characters) when you choose Other.");
            });

            RuleFor(x => x.PurposeNote)
                .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= 200)
                .WithMessage("The purpose details can be at most 200 characters.");

            RuleFor(x => x.TermMonths)
                .GreaterThan(0).WithMessage("Choose the term.");

            RuleFor(x => x.TargetCardId)
                .GreaterThan(0).WithMessage("Choose the card that receives the money.");
        }
    }
}
