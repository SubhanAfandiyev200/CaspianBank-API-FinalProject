namespace Service.Helpers.DTOs.Cards
{
    // Kartın əməliyyat siyahısındakı bir sətir
    public class TransactionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;        // TopUp, TransferOut, TransferIn, Commission, CardFee
        public bool IsIncome { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string Reference { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
