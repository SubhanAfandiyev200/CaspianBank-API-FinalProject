namespace Service.Helpers.DTOs.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;     // Loan, Transfer, Card, Staff
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationFilterDto
    {
        public bool UnreadOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class NotificationPageDto
    {
        public List<NotificationDto> Items { get; set; } = new List<NotificationDto>();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // Brauzerin SignalR hub-ına qoşulması üçün qısa ömürlü bilet (yalnız hub-da keçir, API-də keçmir)
    public class HubTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
