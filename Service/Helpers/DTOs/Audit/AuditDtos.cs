namespace Service.Helpers.DTOs.Audit
{
    public class AuditFilterDto
    {
        public string? Search { get; set; }
        public string? Action { get; set; }     // AuditActions kodlarından biri, boşdursa hamısı
        public DateTime? From { get; set; }     // Bakı günü (daxil)
        public DateTime? To { get; set; }       // Bakı günü (daxil)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class AuditItemDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetLabel { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class AuditPageDto
    {
        public List<AuditItemDto> Items { get; set; } = new List<AuditItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
