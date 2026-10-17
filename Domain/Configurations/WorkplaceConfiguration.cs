using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class WorkplaceConfiguration : IEntityTypeConfiguration<Workplace>
    {
        public void Configure(EntityTypeBuilder<Workplace> builder)
        {
            builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
            builder.Property(m => m.TaxId).IsRequired().HasMaxLength(10);
            builder.Property(m => m.Sector).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Address).IsRequired().HasMaxLength(200);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.TaxId).IsUnique();
        }
    }
}
