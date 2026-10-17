using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class TreasuryEntryConfiguration : IEntityTypeConfiguration<TreasuryEntry>
    {
        public void Configure(EntityTypeBuilder<TreasuryEntry> builder)
        {
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(120);
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(40);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.CreatedAt);
            builder.HasIndex(m => m.Kind);
        }
    }
}
