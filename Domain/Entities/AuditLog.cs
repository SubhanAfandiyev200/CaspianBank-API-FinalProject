using Domain.Common;

namespace Domain.Entities
{
    // Kim, nə vaxt, nə etdi: işçilərin həssas əməliyyatlarının dəyişməz qeydi (yalnız əlavə olunur, dəyişdirilmir və silinmir).
    // Ad və rol həmin anın surətidir: işçi sonra silinsə və ya adı dəyişsə də qeyd olduğu kimi qalır
    public class AuditLog : BaseEntity
    {
        public string? ActorUserId { get; set; }                    // əməliyyatı edən işçi (sistem edibsə boş)
        public string ActorName { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;          // AuditActions: AccountRestricted, LoanApproved, ...
        public string TargetType { get; set; } = string.Empty;      // Customer, Loan, BillProvider, Setting
        public string TargetLabel { get; set; } = string.Empty;     // "Leyla Cəfərova", "KR-2026-000100"
        public string Details { get; set; } = string.Empty;         // qısa izah (səbəb, məbləğ)
    }
}
