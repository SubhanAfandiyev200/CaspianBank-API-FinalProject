using Domain.Enums;

namespace Repository.Results
{
    public enum BillOutcome
    {
        Success,
        ProviderNotFound,       // təchizatçı yoxdur və ya admin onu söndürüb
        SubscriberNotFound,     // belə nömrə və ya abunəçi kodu yoxdur
        AmountOutOfRange,       // məbləğ təchizatçının min-max aralığında deyil (səbəb Message-dədir)
        CardNotFound,
        CardBlocked,
        InsufficientFunds,
        Duplicate,              // eyni Reference artıq işlənib (forma iki dəfə göndərilib)
        Conflict                // eyni anda başqa əməliyyat kartı və ya abunəçini dəyişdi
    }

    // Ödənişin nəticəsi (qəbz üçün lazım olan hər şey burada)
    public class BillResult
    {
        public BillOutcome Outcome { get; set; }
        public string? Message { get; set; }

        public string Reference { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public BillCategory Category { get; set; }
        public string? LogoUrl { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public decimal DebtCleared { get; set; }
        public decimal CreditAdded { get; set; }
        public decimal CashbackEarned { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public decimal CardBalanceAfter { get; set; }
        public decimal SubscriberBalanceAfter { get; set; }     // mənfi = qalan borc, müsbət = balans
        public DateTime PaidAt { get; set; }

        public static BillResult Of(BillOutcome outcome, string? message = null)
        {
            return new BillResult
            {
                Outcome = outcome,
                Message = message
            };
        }
    }

    // Servisin yoxladığı ödəniş sorğusu: repository onu atomik icra edir
    public record BillPaymentPlan(
        string UserId,
        int ProviderId,
        string Identifier,
        decimal Amount,
        int CardId,
        string Reference,
        DateTime UtcNow);
}
