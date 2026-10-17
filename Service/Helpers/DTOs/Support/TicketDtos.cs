namespace Service.Helpers.DTOs.Support
{
    public class CreateTicketDto
    {
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }

    public class ReplyTicketDto
    {
        public string? Message { get; set; }
    }

    public class TicketFilterDto
    {
        public string? Status { get; set; }     // Pending, Answered, Closed; boşdursa hamısı
        public string? Search { get; set; }     // mövzu, müştərinin adı və ya emaili
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Siyahının sətri (həm müştəri, həm işçi üçün)
    public class TicketListItemDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int MessageCount { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;       // işçi siyahısı üçün
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class TicketPageDto
    {
        public List<TicketListItemDto> Items { get; set; } = new List<TicketListItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PendingCount { get; set; }
    }

    public class TicketMessageDto
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TicketDetailDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public List<TicketMessageDto> Messages { get; set; } = new List<TicketMessageDto>();
    }
}
