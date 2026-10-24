using Repository.Results;
using Service.Helpers.Responses;

namespace Service.Helpers
{
    // Repository-nin uğursuz nəticəsini (LoanResult) istifadəçiyə göstərilən xətaya çevirir
    public static class LoanMessages
    {
        public static ServiceResult<T> ToFailure<T>(LoanResult result)
        {
            switch (result.Outcome)
            {
                case LoanOutcome.NotFound:
                    return ServiceResult<T>.NotFound();
                case LoanOutcome.NotAllowed:
                    return ServiceResult<T>.Fail(result.Message ?? "You can't do this.");
                case LoanOutcome.WrongStatus:
                    return ServiceResult<T>.Fail(result.Message ?? "This loan can't be changed right now.");
                case LoanOutcome.OfferExpired:
                    return ServiceResult<T>.Fail("This offer has expired.");
                case LoanOutcome.TargetCardUnavailable:
                    return ServiceResult<T>.Fail(result.Message ?? "The card chosen for the loan is not available.");
                case LoanOutcome.CardNotFound:
                    return ServiceResult<T>.Fail("Choose one of your own cards to pay from.");
                case LoanOutcome.CardBlocked:
                    return ServiceResult<T>.Fail("That card is blocked. Choose another card.");
                case LoanOutcome.CardNotAllowed:
                    return ServiceResult<T>.Fail(result.Message ?? "A loan can't be paid from this card.");
                case LoanOutcome.InsufficientFunds:
                    return ServiceResult<T>.Fail("That card doesn't have enough balance for this payment.");
                case LoanOutcome.NothingDue:
                    return ServiceResult<T>.Fail(result.Message ?? "There is nothing to pay right now.");
                case LoanOutcome.NotPayable:
                    return ServiceResult<T>.Fail(result.Message ?? "This payment is not allowed.");
                case LoanOutcome.TreasuryLow:
                    return ServiceResult<T>.Fail(result.Message ?? "The bank can't pay out this loan right now. Please try again later.");
                case LoanOutcome.Conflict:
                    return ServiceResult<T>.Fail("The balance changed while the payment was made. Please try again.");
                default:
                    return ServiceResult<T>.Fail("Something went wrong. Please try again.");
            }
        }
    }
}
