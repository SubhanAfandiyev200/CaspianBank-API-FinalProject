using Domain.Bills;
using Domain.Entities;
using Repository.Results;
using Service.Helpers.Responses;

namespace Service.Helpers
{
    // Ödəniş xətalarının istifadəçiyə göstərilən mətnləri
    public static class BillMessages
    {
        // Mobil nömrə seçilən operatora aid deyil: düzgün operator qeyd olunmayıb
        public static string WrongOperator(BillProvider chosen, BillProvider? actual)
        {
            if (actual is not null)
            {
                return "The operator is not correct. This number belongs to " + actual.Name + ", not " + chosen.Name + ".";
            }
            return "The operator is not correct. " + chosen.Name + " numbers start with " + BillingEngine.PrefixesText(chosen.Prefixes) + ".";
        }

        public static string SubscriberNotFound(BillProvider provider)
        {
            if (provider.Category == Domain.Enums.BillCategory.Mobile)
            {
                return "This number doesn't exist.";
            }
            return "This " + provider.IdentifierLabel.ToLowerInvariant() + " doesn't exist.";
        }

        public static ServiceResult<T> ToFailure<T>(BillResult result)
        {
            switch (result.Outcome)
            {
                case BillOutcome.ProviderNotFound:
                    return ServiceResult<T>.Fail("This provider isn't available right now.");
                case BillOutcome.SubscriberNotFound:
                    return ServiceResult<T>.Fail("We couldn't find this number or code anymore. Check it and try again.");
                case BillOutcome.AmountOutOfRange:
                    return ServiceResult<T>.Fail(result.Message ?? "The amount is not allowed.");
                case BillOutcome.CardNotFound:
                    return ServiceResult<T>.Fail("Choose one of your own cards to pay from.");
                case BillOutcome.CardBlocked:
                    return ServiceResult<T>.Fail("That card is blocked. Choose another card.");
                case BillOutcome.InsufficientFunds:
                    return ServiceResult<T>.Fail("That card doesn't have enough balance for this payment.");
                case BillOutcome.Conflict:
                    return ServiceResult<T>.Fail("The balance changed while the payment was made. Please try again.");
                default:
                    return ServiceResult<T>.Fail("Something went wrong. Please try again.");
            }
        }
    }
}
