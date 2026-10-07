namespace Service.Helpers.DTOs.Cards
{
    // Növlərin qaydaları (add-card səhifəsi qiymətləri bundan göstərir)
    public class CardTierDto
    {
        public string Tier { get; set; } = string.Empty;
        public decimal IssueFee { get; set; }
        public decimal CashbackPercent { get; set; }
        public decimal TransferLimit { get; set; }
        public decimal CommissionPercent { get; set; }
        public string DesignImage { get; set; } = string.Empty;
    }
}
