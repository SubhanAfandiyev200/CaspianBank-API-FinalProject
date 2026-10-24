using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Müştərinin Caspian-dan kredit müraciəti və krediti. Müraciət zamanı (CreatedAt) Pending olur, işçinin qərarından sonra Active,
    // Offered və ya Rejected olur. Faiz və cərimə dərəcələri müqavilənin surətidir: sonradan OwnBank dəyişsə bu kredit dəyişmir.
    public class Loan : BaseEntity
    {
        public int OwnBankId { get; set; }
        public OwnBank OwnBank { get; set; } = null!;

        public string BorrowerUserId { get; set; } = string.Empty;
        public AppUser Borrower { get; set; } = null!;

        public LoanPurpose Purpose { get; set; }
        public string? PurposeNote { get; set; }            // Purpose = Other olanda məcburidir

        // Ödəniş kodu (məs. KR-2026-000123): Bills səhifəsində kredit bununla tapılır
        public string ContractNumber { get; set; } = string.Empty;

        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }        // işçinin təsdiq etdiyi məbləğ (istənilən məbləğdən az ola bilər)
        public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }         // cari aylıq ödəniş (hissəvi erkən ödənişdən sonra azalır)
        public decimal TotalRepayment { get; set; }         // cədvəl üzrə ümumi ödəniləcək (əsas borc + faiz)
        public decimal OutstandingPrincipal { get; set; }   // qalan əsas borc (ödəniş olunca yenilənir)
        public decimal IncomeAtApply { get; set; }          // müraciət anında maaş

        public decimal InitialAnnualInterestRate { get; set; }  // müqavilə imzalananda faiz
        public decimal AnnualInterestRate { get; set; }         // cari faiz (hissəvi erkən ödənişdə azala bilər)
        public decimal FixedPenalty { get; set; }               // gecikmədə bir dəfəlik cərimə (AZN)
        public decimal DailyPenaltyPercent { get; set; }        // gecikmiş məbləğə gündəlik cərimə (%)

        // Pul bu karta düşür (müştəri müraciətdə seçir: öz kartı, Cashback olmaz)
        public int TargetCardId { get; set; }
        public Card TargetCard { get; set; } = null!;

        public LoanStatus Status { get; set; }
        public string? RejectReason { get; set; }           // daxili səbəb (müştəriyə ümumi mətn göstərilir)
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // Qara siyahıdakı müştərinin krediti iki təsdiq tələb edir: əvvəl kredit şöbəsi (FirstApproved...), sonra Super Admin (ReviewedBy...). İkisi də olmasa pul getmir
        public bool RequiresSuperAdminApproval { get; set; }
        public string? FirstApprovedByUserId { get; set; }
        public DateTime? FirstApprovedAt { get; set; }
        public decimal? FirstApprovedAmount { get; set; }

        public DateTime? OfferExpiresAt { get; set; }       // Offered: təklifin etibarlılıq vaxtı
        public DateTime? AcceptedAt { get; set; }
        public DateTime? DisbursedAt { get; set; }          // pulun karta düşdüyü vaxt
        public DateTime? ClosedAt { get; set; }
        public bool ClosedEarly { get; set; }
        public DateTime? CourtAt { get; set; }

        // Eyni anda iki əməliyyat (ödəniş, qərar) krediti dəyişməsin
        public byte[] RowVersion { get; set; } = null!;

        public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
        public ICollection<LoanPaymentRecord> PaymentRecords { get; set; } = new List<LoanPaymentRecord>();
    }
}
