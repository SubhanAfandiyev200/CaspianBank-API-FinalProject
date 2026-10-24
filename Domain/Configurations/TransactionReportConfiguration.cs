using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class TransactionReportConfiguration : IEntityTypeConfiguration<TransactionReport>
    {
        public void Configure(EntityTypeBuilder<TransactionReport> builder)
        {
            builder.Property(m => m.UserId).IsRequired().HasMaxLength(450);
            builder.Property(m => m.TransactionReference).IsRequired().HasMaxLength(40);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Reason).IsRequired().HasMaxLength(500);
            builder.Property(m => m.ResolvedByName).HasMaxLength(150);
            builder.Property(m => m.ResolutionNote).HasMaxLength(300);
            builder.Property(m => m.ActionTaken).HasMaxLength(200);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.TransactionId).IsUnique();
            builder.HasIndex(m => new { m.UserId, m.CreatedAt });
            builder.HasIndex(m => new { m.Status, m.CreatedAt });
        }
    }
}
