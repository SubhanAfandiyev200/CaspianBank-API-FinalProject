using Domain.Common;

namespace Domain.Entities
{
    // Şübhəli fəaliyyət siqnalı: sistem qaydalara görə özü yaradır, Security işçisi baxıb "təhlükəsizdir" və ya "hesabı dondur" deyir.
    // Eyni hadisə üçün ikinci siqnal yaranmır ((Kind, Reference) unikaldır)
    public class SecurityAlert : BaseEntity
    {
        public string? UserId { get; set; }                         // qeydiyyat cəhdi kimi hesabı olmayan siqnallarda boşdur
        public AppUser? User { get; set; }

        public SecurityAlertKind Kind { get; set; }
        public SecurityAlertSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string Reference { get; set; } = string.Empty;       // hadisənin kodu (TR-..., pəncərə açarı); təkrar siqnalın qarşısı

        public SecurityAlertStatus Status { get; set; } = SecurityAlertStatus.Open;
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public enum SecurityAlertKind
    {
        LargeTransfer,      // tək böyük köçürmə
        RapidTransfers,     // qısa müddətdə çoxlu köçürmə
        DailyVolume,        // bir gündə çox pul göndərilib
        NewAccountOutflow,  // yeni hesabdan nisbətən böyük köçürmə
        AccountLockout,     // təkrarlanan uğursuz girişdən sonra hesab müvəqqəti bloklandı
        DeceasedIdentity    // qeydiyyatda vəfat etmiş vətəndaşın FİN və vəsiqəsi ilə hesab açmağa cəhd (hesab yoxdur, dondurmaq olmur)
    }

    public enum SecurityAlertSeverity
    {
        Medium,
        High
    }

    public enum SecurityAlertStatus
    {
        Open,           // baxılmayıb
        Dismissed,      // baxıldı, təhlükəsizdir
        Frozen          // şübhəli təsdiq olundu, hesab donduruldu
    }
}
