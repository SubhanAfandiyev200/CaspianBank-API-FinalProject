using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Vətəndaşın başqa bankdan götürdüyü kredit (kredit tarixçəsi). Customer ilə ExternalBank arasında many-to-many əlaqədir.
    // Kredit müraciətinə baxan işçi bunları görür: hansı bank, nə qədər, nə qədər ödənilib, gecikmə və gecikmiş məbləğ
    public class ExternalLoan : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public int ExternalBankId { get; set; }
        public ExternalBank ExternalBank { get; set; } = null!;

        public LoanPurpose Purpose { get; set; }
        public string? PurposeNote { get; set; }
        public decimal BorrowedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsClosed { get; set; }
        public int DelayCount { get; set; }                     // neçə dəfə gecikdirilib
        public int MaxDelayDays { get; set; }                   // ən uzun gecikmə (gün)
        public decimal OverdueAmount { get; set; }              // hazırda gecikmiş məbləğ
        public bool WentToCourt { get; set; }                   // məhkəməyə verilib (özü qara siyahı demək deyil)
        public string? Note { get; set; }
    }
}
