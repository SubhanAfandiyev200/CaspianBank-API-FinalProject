using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ExternalBankConfiguration : IEntityTypeConfiguration<ExternalBank>
    {
        public void Configure(EntityTypeBuilder<ExternalBank> builder)
        {
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.LogoUrl).HasMaxLength(200);
            builder.Property(m => m.Bin).IsRequired().HasMaxLength(8);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // BIN bank tanımaq üçün açardır, ona görə unikaldır
            builder.HasIndex(m => m.Bin).IsUnique();
        }
    }
}
