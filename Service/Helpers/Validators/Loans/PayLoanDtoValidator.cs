using Domain.Enums;
using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    public class PayLoanDtoValidator : AbstractValidator<PayLoanDto>
    {
        public PayLoanDtoValidator()
        {
            RuleFor(x => x.CardId)
                .GreaterThan(0).WithMessage("Choose the card to pay from.");

            RuleFor(x => x.Type)
                .Must(t => EnumRules.IsName<LoanPaymentType>(t)).WithMessage("Choose how you want to pay.");

            // Yalnız hissəvi erkən ödənişdə məbləğ yazılır (aylıq ödəniş və tam bağlama məbləği sistem hesablayır)
            When(x => string.Equals(x.Type, nameof(LoanPaymentType.PartialEarly), StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0).WithMessage("Enter the amount you want to pay.")
                    .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");
            });

            // Aylıq ödənişdə məbləğ boş (0) ola bilər: onda bütün borc ödənir. Yazılıbsa hissə-hissə ödəniş olur
            When(x => string.Equals(x.Type, nameof(LoanPaymentType.Installment), StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Amount)
                    .GreaterThanOrEqualTo(0).WithMessage("Enter a valid amount.")
                    .Must(a => decimal.Round(a, 2) == a).WithMessage("Use at most two decimal places.");
            });

            RuleFor(x => x.RequestId)
                .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid request id.");
        }
    }
}
