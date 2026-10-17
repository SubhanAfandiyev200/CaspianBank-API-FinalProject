using CaspianBank_API_FinalProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Service.Helpers.DTOs.Notifications;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Servis qatının "yeni bildiriş var" deməsini SignalR ilə həmin istifadəçinin açıq səhifələrinə çatdırır
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<NotificationHub> _hub;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        public async Task PublishAsync(string userId, NotificationDto notification, int unreadCount)
        {
            // Clients.User: token-dəki NameIdentifier ilə uyğun gələn bütün bağlantılar (bir neçə tab da ola bilər)
            await _hub.Clients.User(userId).SendAsync("notification", new
            {
                notification,
                unreadCount
            });
        }
    }
}
