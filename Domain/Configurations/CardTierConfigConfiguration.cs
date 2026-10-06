using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CardTierConfigConfiguration : IEntityTypeConfiguration<CardTierConfig>
    {
        public void Configure(EntityTypeBuilder<CardTierConfig> builder)
        {
            builder.Property(m => m.IssueFee).HasPrecision(18, 2);
            builder.Property(m => m.CashbackPercent).HasPrecision(18, 2);
            builder.Property(m => m.TransferLimit).HasPrecision(18, 2);
            builder.Property(m => m.CommissionPercent).HasPrecision(18, 2);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Bir növ (Gold və s.) üçün yalnız bir qayda ola bilər: kartın qaydası Tier ilə tapılır
            builder.HasIndex(m => m.Tier).IsUnique();

            // İstifadədə olan dizayn silinməsin
            builder.HasOne(m => m.CardDesign)
                   .WithMany(d => d.TierConfigs)
                   .HasForeignKey(m => m.CardDesignId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
