using Domain.Common;

namespace Domain.Entities
{
    // Home-un yuxarı hissəsi (hero): sol tərəfdəki mətnlər. Tək sətirdir.
    // Düymə ("Get your card") və onun ünvanı statik qalır, kartlar isə CardDesign-dan gəlir (əlaqə yoxdur).
    public class CardHero : BaseEntity
    {
        public string Label { get; set; } = string.Empty;        // başlığın üstündəki kiçik yazı ("Cards in manat")
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
