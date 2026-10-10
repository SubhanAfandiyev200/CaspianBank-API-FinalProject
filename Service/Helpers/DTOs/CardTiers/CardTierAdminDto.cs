namespace Service.Helpers.DTOs.CardTiers
{
    // Admin üçün: kart növünün qaydaları və hansı dizayndan istifadə etdiyi (ictimai CardTierDto-dan fərqli olaraq Id və dizayn adı da var)
    public class CardTierAdminDto
    {
        public int Id { get; set; }
        public string Tier { get; set; } = string.Empty;
        public decimal IssueFee { get; set; }
        public decimal CashbackPercent { get; set; }
        public decimal TransferLimit { get; set; }
        public decimal CommissionPercent { get; set; }
        public int CardDesignId { get; set; }
        public string DesignTitle { get; set; } = string.Empty;
        public string DesignImage { get; set; } = string.Empty;
    }
}
