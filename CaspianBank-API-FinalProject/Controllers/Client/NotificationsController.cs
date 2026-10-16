using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Notifications;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // İstifadəçinin öz bildirişləri (müştəri və işçi eyni endpoint-lərdən istifadə edir). UserId HƏMİŞƏ tokendən götürülür
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Siyahı: ?unreadOnly=true&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] NotificationFilterDto filter)
        {
            return Ok(await _notifications.GetPageAsync(UserId, filter));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            return Ok(new
            {
                count = await _notifications.GetUnreadCountAsync(UserId)
            });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            if (!await _notifications.MarkReadAsync(UserId, id))
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = new[] { "Notification not found." }
                });
            }
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            return Ok(new
            {
                updated = await _notifications.MarkAllReadAsync(UserId)
            });
        }

        // Brauzerin canlı kanala qoşulması üçün 2 dəqiqəlik bilet. MVC bunu istifadəçi adından alıb səhifəyə verir
        [HttpPost("hub-token")]
        public IActionResult CreateHubToken()
        {
            return Ok(_notifications.CreateHubToken(UserId));
        }
    }
}
