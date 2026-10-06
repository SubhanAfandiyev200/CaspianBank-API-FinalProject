using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.Property(m => m.CardNumber).IsRequired().HasMaxLength(16);
            builder.Property(m => m.Balance).HasPrecision(18, 2);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Eyni anda iki əməliyyat balansı pozmasın (eyni versiyanı oxuyub yazmağa çalışan ikincisi xəta alır)
            builder.Property(m => m.RowVersion).IsRowVersion();

            // Kart nömrəsi unikaldır: kartla köçürmə bu nömrə ilə tapılır
            builder.HasIndex(m => m.CardNumber).IsUnique();

            // "İstifadəçinin kartları" sorğusu tez işləsin
            builder.HasIndex(m => m.UserId);

            // İstifadəçi silinəndə kartlar səssizcə yox olmasın
            builder.HasOne(m => m.User)
                   .WithMany(u => u.Cards)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // İstifadədə olan dizayn silinməsin
            builder.HasOne(m => m.CardDesign)
                   .WithMany(d => d.Cards)
                   .HasForeignKey(m => m.CardDesignId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
