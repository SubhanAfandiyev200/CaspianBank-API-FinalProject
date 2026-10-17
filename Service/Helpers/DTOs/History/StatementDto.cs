using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.DTOs.History
{
    // Bir kartın seçilmiş tarix aralığı üçün çıxarışı: açılış balansı + əməliyyatlar = bağlanış balansı
    public class StatementDto
    {
        public int CardId { get; set; }
        public string CardLabel { get; set; } = string.Empty;       // məs. "Standard •••• 9594"
        public string HolderName { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public IEnumerable<TransactionDto> Rows { get; set; } = new List<TransactionDto>();    // köhnədən yeniyə
    }
}
