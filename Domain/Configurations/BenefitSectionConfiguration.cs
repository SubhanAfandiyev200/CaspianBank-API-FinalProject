using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BenefitSectionConfiguration : IEntityTypeConfiguration<BenefitSection>
    {
        public void Configure(EntityTypeBuilder<BenefitSection> builder)
        {
            builder.Property(m => m.Label).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(500);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
