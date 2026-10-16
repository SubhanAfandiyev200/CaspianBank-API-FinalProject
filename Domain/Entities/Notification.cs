using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // İstifadəçiyə (müştəri və ya işçi) göndərilən bildiriş. Bazada saxlanılır və SignalR ilə dərhal brauzerə də ötürülür
    public class Notification : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public NotificationCategory Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }                // saytın daxilində yol (məs. /App/Loans/5), basanda ora gedir

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // Eyni hadisə təkrar yazılmasın deyə (məs. "late:42" bir ödənişin gecikməsi). Boşdursa təkrar yoxlanmır
        public string? DedupeKey { get; set; }
    }
}
