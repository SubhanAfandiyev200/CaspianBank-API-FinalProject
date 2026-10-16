using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifications;
        private readonly IAccountRepository _accounts;
        private readonly INotificationPublisher _publisher;
        private readonly ITokenService _tokens;
        private readonly IValidator<NotificationFilterDto> _filterValidator;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository notifications,
                                   IAccountRepository accounts,
                                   INotificationPublisher publisher,
                                   ITokenService tokens,
                                   IValidator<NotificationFilterDto> filterValidator,
                                   ILogger<NotificationService> logger)
        {
            _notifications = notifications;
            _accounts = accounts;
            _publisher = publisher;
            _tokens = tokens;
            _filterValidator = filterValidator;
            _logger = logger;
        }

        public async Task NotifyAsync(string userId, NotificationCategory category, string title, string message, string? linkUrl = null, string? dedupeKey = null)
        {
            try
            {
                if (dedupeKey is not null && await _notifications.ExistsAsync(userId, dedupeKey))
                {
                    return;
                }

                var notification = new Notification
                {
                    UserId = userId,
                    Category = category,
                    Title = Clip(title, 120),
                    Message = Clip(message, 400),
                    LinkUrl = linkUrl,
                    DedupeKey = dedupeKey
                };

                if (!await _notifications.TryAddAsync(notification))
                {
                    return;
                }

                // Canlı ötürmə uğursuz olsa belə bildiriş bazadadır: səhifəni açanda görünəcək
                var unread = await _notifications.CountUnreadAsync(userId);
                await _publisher.PublishAsync(userId, ToDto(notification), unread);
            }
            catch (Exception ex)
            {
                // Bildiriş əsas əməliyyatı (kredit, köçürmə) pozmamalıdır
                _logger.LogWarning(ex, "Bildiriş yazıla bilmədi: istifadəçi {UserId}, başlıq {Title}", userId, title);
            }
        }

        public async Task NotifyRolesAsync(string[] roles, NotificationCategory category, string title, string message, string? linkUrl = null, string? dedupeKey = null)
        {
            try
            {
                var userIds = await _accounts.GetUserIdsInRolesAsync(roles);
                foreach (var userId in userIds)
                {
                    await NotifyAsync(userId, category, title, message, linkUrl, dedupeKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "İşçilərə bildiriş göndərilə bilmədi: {Title}", title);
            }
        }

        public async Task<NotificationPageDto> GetPageAsync(string userId, NotificationFilterDto filter)
        {
            await _filterValidator.EnsureValidAsync(filter);

            var (items, total) = await _notifications.GetPageAsync(userId, filter.UnreadOnly, (filter.Page - 1) * filter.PageSize, filter.PageSize);
            return new NotificationPageDto
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = total,
                UnreadCount = await _notifications.CountUnreadAsync(userId),
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _notifications.CountUnreadAsync(userId);
        }

        public async Task<bool> MarkReadAsync(string userId, int id)
        {
            return await _notifications.MarkReadAsync(id, userId);
        }

        public async Task<int> MarkAllReadAsync(string userId)
        {
            return await _notifications.MarkAllReadAsync(userId);
        }

        public HubTokenDto CreateHubToken(string userId)
        {
            var (token, expiresAt) = _tokens.CreateHubToken(userId);
            return new HubTokenDto
            {
                Token = token,
                ExpiresAt = expiresAt
            };
        }

        private static NotificationDto ToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Category = notification.Category.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                LinkUrl = notification.LinkUrl,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        // Baza sütununun uzunluğundan uzun mətn yazma xətası verməsin
        private static string Clip(string text, int max)
        {
            if (text.Length <= max)
            {
                return text;
            }
            return text.Substring(0, max - 1) + "…";
        }
    }
}
