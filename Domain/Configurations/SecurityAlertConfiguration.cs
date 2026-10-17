using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class SecurityAlertConfiguration : IEntityTypeConfiguration<SecurityAlert>
    {
        public void Configure(EntityTypeBuilder<SecurityAlert> builder)
        {
            builder.Property(m => m.UserId).IsRequired().HasMaxLength(450);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(300);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(60);
            builder.Property(m => m.ResolvedByName).HasMaxLength(150);
            builder.Property(m => m.ResolutionNote).HasMaxLength(300);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => new { m.Kind, m.Reference }).IsUnique();
            builder.HasIndex(m => new { m.Status, m.CreatedAt });
        }
    }
}
