namespace Service.Helpers.DTOs.Cards
{
    // Kartların siyahısı üçün: tam nömrə YOXDUR, yalnız son 4 rəqəm
    public class CardDto
    {
        public int Id { get; set; }
        public string Tier { get; set; } = string.Empty;        // Cashback / Standard / Silver / Gold
        public string Last4 { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;      // MM/yy
        public decimal Balance { get; set; }
        public bool IsBlocked { get; set; }
        public string HolderName { get; set; } = string.Empty;
        public string DesignImage { get; set; } = string.Empty; // /images/cards/xxx.png
    }
}
