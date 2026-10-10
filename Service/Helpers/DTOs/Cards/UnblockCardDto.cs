namespace Service.Helpers.DTOs.Cards
{
    // Kartı blokdan çıxarmaq: emailə gələn 6 rəqəmli kod
    public class UnblockCardDto
    {
        public string Code { get; set; } = string.Empty;
    }
}
