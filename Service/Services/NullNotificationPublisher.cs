using Service.Helpers.DTOs.Notifications;
using Service.Services.Interfaces;

namespace Service.Services
{
    // Canlı kanal qurulmayıbsa (məs. başqa host) bildiriş yenə bazaya yazılır, sadəcə dərhal göstərilmir
    public class NullNotificationPublisher : INotificationPublisher
    {
        public Task PublishAsync(string userId, NotificationDto notification, int unreadCount)
        {
            return Task.CompletedTask;
        }
    }
}
