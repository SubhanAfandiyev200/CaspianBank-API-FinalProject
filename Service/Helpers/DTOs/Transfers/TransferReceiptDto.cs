namespace Service.Helpers.DTOs.Transfers
{
    public class TransferReceiptDto
    {
        public string Reference { get; set; } = string.Empty;
        public string FromLabel { get; set; } = string.Empty;
        public string ToLabel { get; set; } = string.Empty;
        public string? ToHolder { get; set; }
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public decimal Total { get; set; }
        public string? Note { get; set; }
        public decimal FromBalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
