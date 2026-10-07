namespace Service.Helpers.DTOs.Cards
{
    public class AddCardDto
    {
        public string Tier { get; set; } = string.Empty;        // Standard / Silver / Gold
        public int FundingCardId { get; set; }                  // açılış haqqı hansı kartdan çıxılsın
    }
}
