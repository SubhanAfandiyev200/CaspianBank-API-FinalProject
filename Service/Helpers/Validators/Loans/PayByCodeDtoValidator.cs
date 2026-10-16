using FluentValidation;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers.Validators.Loans
{
    public class PayByCodeDtoValidator : AbstractValidator<PayByCodeDto>
    {
        public PayByCodeDtoValidator()
        {
            RuleFor(x => x.ContractNumber)
                .Matches(@"^KR-\d{4}-\d{6}$").WithMessage("Enter the loan code like KR-2026-000123.");

            RuleFor(x => x.CardId)
                .GreaterThan(0).WithMessage("Choose the card to pay from.");

            RuleFor(x => x.RequestId)
                .Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid request id.");
        }
    }
}
