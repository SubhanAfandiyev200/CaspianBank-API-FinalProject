namespace Service.Helpers.DTOs.Security
{
    public class SecurityAlertFilterDto
    {
        public string? Status { get; set; }         // Open, Dismissed, Frozen
        public string? Severity { get; set; }       // Medium, High
        public string? Kind { get; set; }           // SecurityAlertKind adı
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SecurityAlertDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool AccountRestricted { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class SecurityAlertPageDto
    {
        public List<SecurityAlertDto> Items { get; set; } = new List<SecurityAlertDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int OpenCount { get; set; }
        public int OpenHighCount { get; set; }
        public int ResolvedThisWeek { get; set; }
    }

    public class ResolveAlertDto
    {
        public string Outcome { get; set; } = string.Empty;     // Dismiss (təhlükəsizdir) və ya Freeze (hesabı dondur)
        public string? Note { get; set; }                       // Freeze üçün məcburidir (dondurma səbəbi kimi yazılır)
    }
}
