using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class RecurringPaymentConfiguration : IEntityTypeConfiguration<RecurringPayment>
    {
        public void Configure(EntityTypeBuilder<RecurringPayment> builder)
        {
            builder.Property(m => m.Identifier).IsRequired().HasMaxLength(30);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.LastPeriod).IsRequired().HasMaxLength(7);
            builder.Property(m => m.LastAttemptDay).HasMaxLength(10);
            builder.Property(m => m.LastError).HasMaxLength(300);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Müştərinin siyahısı və gündəlik iş (aktivlər, bu ay hələ işlənməyənlər)
            builder.HasIndex(m => m.UserId);
            builder.HasIndex(m => new { m.IsActive, m.LastPeriod });

            // Təkrarlanan ödəniş müştərinin öz ayarıdır: istifadəçi silinsə o da silinir. Kart və təchizatçı silinməsin
            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Card).WithMany().HasForeignKey(m => m.CardId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Provider).WithMany().HasForeignKey(m => m.ProviderId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
