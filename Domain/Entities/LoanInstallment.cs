using Domain.Common;

namespace Domain.Entities
{
    // Kreditin bir aylıq ödənişi. DueDate ayın son günüdür.
    // PaidAmount yalnız faiz + əsas borcun ödənilən hissəsidir (əvvəl faiz, sonra əsas borc); cərimə ayrıca PaidPenalty-dədir.
    public class LoanInstallment : BaseEntity
    {
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        public int Number { get; set; }
        public DateTime DueDate { get; set; }

        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? FullyPaidAt { get; set; }

        // Cərimə: gecikmənin 1-ci günündən sabit məbləğ (FixedPenaltyAmount) + hər gün ödənilməmiş məbləğin faizi (AccruedPenalty)
        public decimal FixedPenaltyAmount { get; set; }
        public decimal AccruedPenalty { get; set; }
        public decimal PaidPenalty { get; set; }
        public DateTime? PenaltyUpdatedAt { get; set; }     // gündəlik cərimənin hansı günə qədər hesablandığı

        // Kredit vaxtından əvvəl tam bağlananda gələcək aylar ləğv olunur
        public bool IsCancelled { get; set; }
    }
}
