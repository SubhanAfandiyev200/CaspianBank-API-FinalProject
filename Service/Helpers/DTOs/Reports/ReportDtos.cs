namespace Service.Helpers.DTOs.Reports
{
    // Müştəri: əməliyyatı bildirir
    public class ReportTransactionDto
    {
        public int TransactionId { get; set; }
        public string? Reason { get; set; }
    }

    // Müştərinin öz bildirişləri
    public class MyReportDto
    {
        public int Id { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class ReportFilterDto
    {
        public string? Status { get; set; }         // Pending, Reviewed, ActionTaken, Dismissed
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // İşçi: bildiriş siyahısının sətri
    public class AdminReportDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ProposedAction { get; set; }     // "Take action" basılsa nə olacaq (yalnız baxılmamışlarda)
        public string? ActionTaken { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class AdminReportPageDto
    {
        public List<AdminReportDto> Items { get; set; } = new List<AdminReportDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int PendingCount { get; set; }
        public int ResolvedThisWeek { get; set; }
    }

    public class ResolveReportDto
    {
        public string Outcome { get; set; } = string.Empty;     // Review, Action, Dismiss
        public string? Note { get; set; }                       // Action üçün məcburidir (səbəb kimi yazılır)
    }
}
