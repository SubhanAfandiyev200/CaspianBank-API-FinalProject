using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(m => m.ActorUserId).HasMaxLength(450);
            builder.Property(m => m.ActorName).IsRequired().HasMaxLength(120);
            builder.Property(m => m.ActorRole).IsRequired().HasMaxLength(50);
            builder.Property(m => m.Action).IsRequired().HasMaxLength(60);
            builder.Property(m => m.TargetType).IsRequired().HasMaxLength(40);
            builder.Property(m => m.TargetLabel).IsRequired().HasMaxLength(150);
            builder.Property(m => m.Details).IsRequired().HasMaxLength(400);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Siyahı ən yenidən köhnəyə, əməliyyat və işçi üzrə süzülür. İstifadəçiyə bağlı xarici açar yoxdur: işçi silinsə qeyd qalır
            builder.HasIndex(m => m.CreatedAt);
            builder.HasIndex(m => new { m.Action, m.CreatedAt });
        }
    }
}
