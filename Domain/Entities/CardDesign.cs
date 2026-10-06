using Domain.Common;

namespace Domain.Entities
{
    // Kartın fon dizaynı (şəkil). Home səhifəsindəki kart yelpazəsində göstərilir.
    // Admin istədiyi qədər dizayn əlavə edə, göstərə/gizlədə və sıralaya bilər.
    public class CardDesign : BaseEntity
    {
        public string Title { get; set; } = string.Empty;   // adminin gördüyü ad (məs. "Regular")
        public string Image { get; set; } = string.Empty;   // wwwroot-dakı yol (/images/cards/xxx.png)
        public bool ShowOnHome { get; set; } = true;
        public int DisplayOrder { get; set; }               // kiçik rəqəm öndə görünür

        // Bu dizaynı istifadə edən kartlar və növ qaydaları (one-to-many)
        public ICollection<Card> Cards { get; set; } = new List<Card>();
        public ICollection<CardTierConfig> TierConfigs { get; set; } = new List<CardTierConfig>();
    }
}
