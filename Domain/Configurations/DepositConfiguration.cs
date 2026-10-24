using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class DepositPlanConfiguration : IEntityTypeConfiguration<DepositPlan>
    {
        public void Configure(EntityTypeBuilder<DepositPlan> builder)
        {
            builder.Property(m => m.AnnualRate).HasPrecision(5, 2);
            builder.Property(m => m.MinAmount).HasPrecision(18, 2);
            builder.Property(m => m.MaxAmount).HasPrecision(18, 2);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasIndex(m => m.TermMonths).IsUnique();
        }
    }

    public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
    {
        public void Configure(EntityTypeBuilder<Deposit> builder)
        {
            builder.Property(m => m.UserId).IsRequired().HasMaxLength(450);
            builder.Property(m => m.AnnualRate).HasPrecision(5, 2);
            builder.Property(m => m.Principal).HasPrecision(18, 2);
            builder.Property(m => m.ExpectedInterest).HasPrecision(18, 2);
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(30);

            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Plan).WithMany().HasForeignKey(m => m.PlanId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(m => m.SourceCard).WithMany().HasForeignKey(m => m.SourceCardId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.PayoutCard).WithMany().HasForeignKey(m => m.PayoutCardId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => m.Reference).IsUnique();
            builder.HasIndex(m => new { m.UserId, m.Status });
            builder.HasIndex(m => new { m.Status, m.MaturesAt });
        }
    }
}
