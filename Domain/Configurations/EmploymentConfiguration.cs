using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class EmploymentConfiguration : IEntityTypeConfiguration<Employment>
    {
        public void Configure(EntityTypeBuilder<Employment> builder)
        {
            builder.Property(m => m.Position).IsRequired().HasMaxLength(100);
            builder.Property(m => m.MonthlySalary).HasPrecision(18, 2);
            builder.Property(m => m.LeaveReason).HasMaxLength(200);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.CustomerId);

            builder.HasOne(m => m.Customer)
                   .WithMany(c => c.Employments)
                   .HasForeignKey(m => m.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Workplace)
                   .WithMany(w => w.Employments)
                   .HasForeignKey(m => m.WorkplaceId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
