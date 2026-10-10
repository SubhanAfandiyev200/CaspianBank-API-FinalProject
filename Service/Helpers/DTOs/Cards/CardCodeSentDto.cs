namespace Service.Helpers.DTOs.Cards
{
    // Kart əməliyyatı üçün kod emailə göndərildi: istifadəçiyə hara getdiyi göstərilir
    public class CardCodeSentDto
    {
        public string MaskedEmail { get; set; } = string.Empty;

        // Yalnız SMTP qurulmayıb və Development rejimidir: kod ekranda göstərilsin deyə qaytarılır
        public string? DevCode { get; set; }
    }
}
