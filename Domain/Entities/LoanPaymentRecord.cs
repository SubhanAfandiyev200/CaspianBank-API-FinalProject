using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Kreditin ödəniş tarixçəsi. Bir ödəniş bir neçə aylıq ödənişi örtürsə, hər biri üçün ayrıca sətir yazılır (eyni TransactionReference ilə)
    public class LoanPaymentRecord : BaseEntity
    {
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        public int? InstallmentId { get; set; }             // erkən ödənişlərdə boşdur
        public LoanPaymentType Type { get; set; }

        public decimal Amount { get; set; }                 // PenaltyPart + InterestPart + PrincipalPart
        public decimal PenaltyPart { get; set; }
        public decimal InterestPart { get; set; }
        public decimal PrincipalPart { get; set; }
        public decimal? InterestRateAfter { get; set; }     // hissəvi erkən ödənişdən sonra yeni faiz

        public DateTime PaidAt { get; set; }
        public string CardNumber { get; set; } = string.Empty;          // ödənişin çıxdığı kart (tarixçə üçün surət)
        public string TransactionReference { get; set; } = string.Empty; // kart jurnalındakı Reference ilə eynidir
    }
}
