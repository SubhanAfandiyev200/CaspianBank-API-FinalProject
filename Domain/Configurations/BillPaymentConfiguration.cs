using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
    {
        public void Configure(EntityTypeBuilder<BillPayment> builder)
        {
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(30);
            builder.Property(m => m.ProviderName).IsRequired().HasMaxLength(60);
            builder.Property(m => m.Identifier).IsRequired().HasMaxLength(20);
            builder.Property(m => m.SubscriberName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.DebtCleared).HasPrecision(18, 2);
            builder.Property(m => m.CreditAdded).HasPrecision(18, 2);
            builder.Property(m => m.CashbackEarned).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Eyni sorğu (RequestId) ikinci dəfə gəlsə pul bir dəfə çıxır: Reference unikaldır
            builder.HasIndex(m => m.Reference).IsUnique();
            builder.HasIndex(m => new { m.UserId, m.CreatedAt });

            // Ödəniş tarixçəsi qalmalıdır: istifadəçi, kart, təchizatçı və abunəçi silinəndə ödəniş silinmir
            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Card).WithMany().HasForeignKey(m => m.CardId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Provider).WithMany().HasForeignKey(m => m.ProviderId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Subscriber).WithMany().HasForeignKey(m => m.SubscriberId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
