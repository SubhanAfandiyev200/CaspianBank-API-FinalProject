using Domain.Entities;
using Domain.Enums;

namespace Repository.Results
{
    public enum LoanOutcome
    {
        Success,
        NotFound,
        NotAllowed,             // başqasının krediti və ya bu əməliyyat bu istifadəçiyə icazəli deyil
        WrongStatus,            // kreditin vəziyyəti bu əməliyyata uyğun deyil (məs. artıq qərar verilib)
        OfferExpired,           // təklifin vaxtı bitib
        TargetCardUnavailable,  // pul düşəcək kart yoxdur, bloklanıb və ya Cashback-dir
        CardNotFound,
        CardBlocked,
        CardNotAllowed,         // Cashback kartından ödəniş olmur
        InsufficientFunds,
        NothingDue,             // ödəniləcək məbləğ yoxdur
        NotPayable,             // erkən ödəniş qaydalarına uyğun deyil (səbəb Message-dədir)
        Duplicate,              // eyni Reference artıq işlənib (forma iki dəfə göndərilib)
        Conflict,               // eyni anda başqa əməliyyat krediti və ya kartı dəyişdi
        TreasuryLow             // bankın xəzinəsində bu krediti verməyə kifayət qədər vəsait yoxdur
    }

    // Kredit əməliyyatlarının nəticəsi. Pul əməliyyatlarında ödənilən hissələr və kartın yeni balansı da buradadır (qəbz üçün)
    public class LoanResult
    {
        public LoanOutcome Outcome { get; set; }
        public string? Message { get; set; }
        public int LoanId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public LoanStatus Status { get; set; }

        public LoanPaymentType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal PenaltyPart { get; set; }
        public decimal InterestPart { get; set; }
        public decimal PrincipalPart { get; set; }
        public decimal CardBalanceAfter { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public bool LoanClosed { get; set; }
        public decimal? NewInterestRate { get; set; }
        public DateTime PaidAt { get; set; }

        public static LoanResult Of(LoanOutcome outcome, string? message = null)
        {
            return new LoanResult
            {
                Outcome = outcome,
                Message = message
            };
        }
    }

    // Servisin yoxladığı ödəniş sorğusu: repository onu atomik icra edir.
    // OwnerOnly = true olanda yalnız kreditin sahibi ödəyə bilər (erkən ödənişlər); ödəniş kodu ilə aylıq ödənişi hər kəs ödəyə bilər
    public record LoanPaymentPlan(
        string PayerUserId,
        int LoanId,
        int CardId,
        LoanPaymentType Type,
        decimal Amount,
        string Reference,
        bool OwnerOnly,
        DateTime UtcNow);

    // Gündəlik işdən sonra müştəriyə deyilməli olan hadisələr (bildiriş yazmaq servisin işidir)
    public enum LoanNoticeKind
    {
        OfferExpired,   // təklifin vaxtı bitdi
        DueSoon,        // ödəniş tezliklə (bir neçə gün içində) olacaq
        Overdue,        // ödəniş gecikib
        Court           // kredit məhkəməyə keçdi
    }

    // Bir bildirişlik hadisə. Key eyni hadisənin təkrar bildirilməməsi üçündür (məs. "late:42")
    public record LoanNotice(
        LoanNoticeKind Kind,
        string UserId,
        int LoanId,
        string ContractNumber,
        string Key,
        decimal Amount,
        DateTime? DueDate,
        int Days);

    public class LoanMaintenanceResult
    {
        public int Changed { get; set; }
        public IReadOnlyList<LoanNotice> Notices { get; set; } = new List<LoanNotice>();
    }

    // İşçi siyahısının bir səhifəsi
    public class LoanReviewPage
    {
        public IReadOnlyList<Loan> Items { get; set; } = new List<Loan>();
        public int TotalCount { get; set; }
    }
}
