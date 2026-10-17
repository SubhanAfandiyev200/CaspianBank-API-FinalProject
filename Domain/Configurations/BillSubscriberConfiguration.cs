using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BillSubscriberConfiguration : IEntityTypeConfiguration<BillSubscriber>
    {
        public void Configure(EntityTypeBuilder<BillSubscriber> builder)
        {
            builder.Property(m => m.Identifier).IsRequired().HasMaxLength(20);
            builder.Property(m => m.FullName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.PackageName).HasMaxLength(40);
            builder.Property(m => m.Balance).HasPrecision(18, 2);
            builder.Property(m => m.MonthlyFee).HasPrecision(18, 2);
            builder.Property(m => m.AverageUsage).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(m => m.RowVersion).IsRowVersion();

            // Bir təchizatçıda eyni nömrə və ya kod iki dəfə olmaz: axtarış bununla tapılır
            builder.HasIndex(m => new { m.ProviderId, m.Identifier }).IsUnique();

            builder.HasOne(m => m.Provider)
                   .WithMany(p => p.Subscribers)
                   .HasForeignKey(m => m.ProviderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
