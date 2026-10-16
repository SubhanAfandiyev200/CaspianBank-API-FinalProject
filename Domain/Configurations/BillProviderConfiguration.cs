using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BillProviderConfiguration : IEntityTypeConfiguration<BillProvider>
    {
        public void Configure(EntityTypeBuilder<BillProvider> builder)
        {
            builder.Property(m => m.Code).IsRequired().HasMaxLength(30);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(60);
            builder.Property(m => m.LogoUrl).HasMaxLength(300);
            builder.Property(m => m.Prefixes).HasMaxLength(40);
            builder.Property(m => m.IdentifierLabel).IsRequired().HasMaxLength(40);
            builder.Property(m => m.UnitName).HasMaxLength(10);
            builder.Property(m => m.UnitPrice).HasPrecision(18, 4);
            builder.Property(m => m.MinPayment).HasPrecision(18, 2);
            builder.Property(m => m.MaxPayment).HasPrecision(18, 2);

            // SQL skriptindən (Seed/bills.sql) əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.Code).IsUnique();
        }
    }
}
