using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BillChargeConfiguration : IEntityTypeConfiguration<BillCharge>
    {
        public void Configure(EntityTypeBuilder<BillCharge> builder)
        {
            builder.Property(m => m.Period).IsRequired().HasMaxLength(7);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(120);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Usage).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Bir abunəçiyə bir ay yalnız bir hesab yazılır (gündəlik iş təkrar işləsə də)
            builder.HasIndex(m => new { m.SubscriberId, m.Period }).IsUnique();

            builder.HasOne(m => m.Subscriber)
                   .WithMany(s => s.Charges)
                   .HasForeignKey(m => m.SubscriberId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
