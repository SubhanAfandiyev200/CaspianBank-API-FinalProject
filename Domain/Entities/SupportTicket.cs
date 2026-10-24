using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Müştərinin dəstək xidmətinə yazdığı mövzu. Yazışma SupportMessage sətirləridir (müştəri və işçi növbə ilə)
    public class SupportTicket : BaseEntity
    {
        public string? UserId { get; set; }                 // qeydiyyatsız ziyarətçinin mesajında boşdur (GuestEmail dolur)
        public AppUser? User { get; set; }
        public string? GuestEmail { get; set; }             // qeydiyyatsız ziyarətçinin emaili: işçi cavabı bu ünvana email kimi gedir

        public string Subject { get; set; } = string.Empty;
        public TicketStatus Status { get; set; } = TicketStatus.Pending;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;     // son mesajın vaxtı: siyahı buna görə sıralanır

        public List<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
    }
}
