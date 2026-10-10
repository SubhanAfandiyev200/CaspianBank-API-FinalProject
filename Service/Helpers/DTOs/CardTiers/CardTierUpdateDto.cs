namespace Service.Helpers.DTOs.CardTiers
{
    // Növün adı (Cashback, Standard, Silver, Gold) dəyişmir: kod ona görə qayda tapır. Yalnız qiymətlər və dizayn dəyişir
    public class CardTierUpdateDto
    {
        // Kartın bir dəfəlik açılış haqqı (AZN)
        public decimal? IssueFee { get; set; }

        // Ödənişlərdən qayıdan cashback faizi
        public decimal? CashbackPercent { get; set; }

        // Bu məbləğə qədər köçürmədə komissiya yoxdur (AZN)
        public decimal? TransferLimit { get; set; }

        // Limitdən yuxarı hissəyə tətbiq olunan komissiya faizi
        public decimal? CommissionPercent { get; set; }

        public int? CardDesignId { get; set; }
    }
}
