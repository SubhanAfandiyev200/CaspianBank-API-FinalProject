using Domain.Common;

namespace Domain.Entities
{
    // Müştərinin "bu əməliyyat şübhəlidir" bildirişi (History-dəki Report düyməsi). Bir əməliyyat bir dəfə bildirilə bilir.
    // Əməliyyatın təsviri və məbləği bildirildiyi andakı surətdir
    public class TransactionReport : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;          // bildirən müştəri
        public AppUser User { get; set; } = null!;

        public int TransactionId { get; set; }                      // bildirilən kart əməliyyatı (unikal: bir dəfə)
        public string TransactionReference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionAt { get; set; }

        public string Reason { get; set; } = string.Empty;          // müştərinin izahı

        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public string? ActionTaken { get; set; }                    // "Account of X restricted" / "Card 1234 blocked"
    }

    public enum ReportStatus
    {
        Pending,        // baxılmayıb
        Reviewed,       // baxıldı, əlavə addım lazım olmadı
        ActionTaken,    // tədbir görüldü (hesab donduruldu və ya kart bloklandı)
        Dismissed       // əsassız sayıldı
    }
}
