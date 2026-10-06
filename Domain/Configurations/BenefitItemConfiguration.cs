using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BenefitItemConfiguration : IEntityTypeConfiguration<BenefitItem>
    {
        public void Configure(EntityTypeBuilder<BenefitItem> builder)
        {
            builder.Property(m => m.Label).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(300);
            builder.Property(m => m.ButtonText).IsRequired().HasMaxLength(100);
            builder.Property(m => m.ButtonUrl).IsRequired().HasMaxLength(300);
            builder.Property(m => m.Text1).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Text2).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Text3).IsRequired().HasMaxLength(200);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
