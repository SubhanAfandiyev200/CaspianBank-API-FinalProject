using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CardDesignConfiguration : IEntityTypeConfiguration<CardDesign>
    {
        public void Configure(EntityTypeBuilder<CardDesign> builder)
        {
            builder.Property(m => m.Title).IsRequired().HasMaxLength(50);
            builder.Property(m => m.Image).IsRequired().HasMaxLength(300);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => new { m.ShowOnHome, m.DisplayOrder });

            // Başlanğıc dizaynlar: Home səhifəsi ilk gündən boş qalmasın (şəkillər wwwroot/images/cards/-dadır)
            var seededAt = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
            builder.HasData(
                new CardDesign { Id = 1, Title = "Regular", Image = "/images/cards/regular.png", ShowOnHome = true, DisplayOrder = 1, CreatedAt = seededAt },
                new CardDesign { Id = 2, Title = "Silver", Image = "/images/cards/silver.png", ShowOnHome = true, DisplayOrder = 2, CreatedAt = seededAt },
                new CardDesign { Id = 3, Title = "Gold", Image = "/images/cards/gold.png", ShowOnHome = true, DisplayOrder = 3, CreatedAt = seededAt },
                // Cashback kartının dizaynı: Home yelpazəsində göstərilmir, yalnız App-da
                new CardDesign { Id = 4, Title = "Cashback", Image = "/images/cards/cashback.png", ShowOnHome = false, DisplayOrder = 4, CreatedAt = seededAt });
        }
    }
}
