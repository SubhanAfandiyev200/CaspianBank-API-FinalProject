namespace Service.Helpers.DTOs.Cards
{
    // Balansı artırmaq: pul "xarici bank kartından" gəlir (simulyasiya). Bu kart məlumatları heç yerdə saxlanılmır.
    public class TopUpDto
    {
        public decimal Amount { get; set; }
        public string SourceCardNumber { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;      // MM/yy
        public string Cvv { get; set; } = string.Empty;
    }
}
