using Service.Helpers.DTOs.Notifications;

namespace Service.Services.Interfaces
{
    // Yeni bildirişi canlı (brauzerə) ötürür. Servis qatı SignalR-ı tanımır: real icra API layihəsindədir (SignalRNotificationPublisher)
    public interface INotificationPublisher
    {
        Task PublishAsync(string userId, NotificationDto notification, int unreadCount);
    }
}
