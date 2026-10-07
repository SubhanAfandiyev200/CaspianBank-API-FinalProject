using Domain.Entities;
using Domain.Enums;
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

            // Başlanğıc qaydalar: kart açma bunlarsız işləməz (admin sonra dəyişə bilər). CardDesignId: 1 Regular, 2 Silver, 3 Gold, 4 Cashback
            var seededAt = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
            builder.HasData(
                new CardTierConfig { Id = 1, Tier = CardTier.Cashback, IssueFee = 0m, CashbackPercent = 0m, TransferLimit = 0m, CommissionPercent = 0m, CardDesignId = 4, CreatedAt = seededAt },
                new CardTierConfig { Id = 2, Tier = CardTier.Standard, IssueFee = 10m, CashbackPercent = 0.5m, TransferLimit = 500m, CommissionPercent = 1m, CardDesignId = 1, CreatedAt = seededAt },
                new CardTierConfig { Id = 3, Tier = CardTier.Silver, IssueFee = 15m, CashbackPercent = 1m, TransferLimit = 2000m, CommissionPercent = 0.6m, CardDesignId = 2, CreatedAt = seededAt },
                new CardTierConfig { Id = 4, Tier = CardTier.Gold, IssueFee = 40m, CashbackPercent = 1.5m, TransferLimit = 10000m, CommissionPercent = 0.3m, CardDesignId = 3, CreatedAt = seededAt });
        }
    }
}
