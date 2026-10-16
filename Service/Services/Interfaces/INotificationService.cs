using Domain.Enums;
using Service.Helpers.DTOs.Notifications;

namespace Service.Services.Interfaces
{
    public interface INotificationService
    {
        // ---------- Yazmaq (digər servislər çağırır) ----------

        // Bildiriş yazır və canlı göndərir. Xəta olsa əsas əməliyyatı pozmur (loglanır).
        // dedupeKey verilibsə və bu istifadəçiyə eyni açarla bildiriş artıq varsa, təkrar yazılmır
        Task NotifyAsync(string userId, NotificationCategory category, string title, string message, string? linkUrl = null, string? dedupeKey = null);

        // Verilən rollardakı bütün işçilərə (məs. yeni kredit müraciəti Accountant, Admin, SuperAdmin-ə)
        Task NotifyRolesAsync(string[] roles, NotificationCategory category, string title, string message, string? linkUrl = null, string? dedupeKey = null);

        // ---------- İstifadəçinin öz bildirişləri ----------

        Task<NotificationPageDto> GetPageAsync(string userId, NotificationFilterDto filter);
        Task<int> GetUnreadCountAsync(string userId);

        // Bildiriş yoxdursa və ya başqasınındırsa false
        Task<bool> MarkReadAsync(string userId, int id);
        Task<int> MarkAllReadAsync(string userId);

        // Brauzerin hub-a qoşulması üçün qısa ömürlü bilet
        HubTokenDto CreateHubToken(string userId);
    }
}
