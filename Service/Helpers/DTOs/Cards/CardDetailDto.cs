namespace Service.Helpers.DTOs.Cards
{
    // Tək kartın səhifəsi üçün: sahibinə tam nömrə və növün qaydaları göstərilir
    public class CardDetailDto : CardDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public decimal IssueFee { get; set; }
        public decimal CashbackPercent { get; set; }
        public decimal TransferLimit { get; set; }
        public decimal CommissionPercent { get; set; }
    }
}
