using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class RecurringTransferConfiguration : IEntityTypeConfiguration<RecurringTransfer>
    {
        public void Configure(EntityTypeBuilder<RecurringTransfer> builder)
        {
            builder.Property(m => m.UserId).IsRequired().HasMaxLength(450);
            builder.Property(m => m.ToCardNumber).HasMaxLength(19);
            builder.Property(m => m.ToLabel).IsRequired().HasMaxLength(120);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Note).HasMaxLength(140);
            builder.Property(m => m.LastPeriod).IsRequired().HasMaxLength(7);
            builder.Property(m => m.LastAttemptDay).HasMaxLength(10);
            builder.Property(m => m.LastError).HasMaxLength(300);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.SourceCard).WithMany().HasForeignKey(m => m.SourceCardId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.UserId, m.IsActive });
            builder.HasIndex(m => new { m.IsActive, m.LastPeriod });
        }
    }
}
