namespace Service.Helpers.DTOs.Transfers
{
    public class CreateRecurringTransferDto
    {
        public int FromCardId { get; set; }
        public int? ToCardId { get; set; }              // öz kartına
        public string? ToCardNumber { get; set; }       // başqa müştərinin və ya başqa bankın kartı
        public decimal Amount { get; set; }
        public int DayOfMonth { get; set; }             // 1-28
        public string? Note { get; set; }
    }

    public class RecurringTransferDto
    {
        public int Id { get; set; }
        public string FromLabel { get; set; } = string.Empty;       // Standard •••• 1234
        public string ToLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int DayOfMonth { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; }
        public DateTime NextRunDate { get; set; }                   // Bakı günü
        public string? LastError { get; set; }
    }
}
