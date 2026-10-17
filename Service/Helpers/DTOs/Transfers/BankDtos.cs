namespace Service.Helpers.DTOs.Transfers
{
    // Kart nömrəsinin ilk 8 rəqəminə görə tanınan bank (köçürmə formasında nömrə sahəsinin üstündəki işarə)
    public class BankLookupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }        // Caspian üçün boşdur (sayt loqosu göstərilir)
        public bool IsCaspian { get; set; }
    }

    // Kartın bu ayki limit vəziyyəti: formada komissiya canlı hesablanır
    public class CardLimitDto
    {
        public int CardId { get; set; }
        public string Tier { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal UsedThisMonth { get; set; }      // bu ay bu kartdan göndərilən (komissiya daxil deyil)
    }
}
