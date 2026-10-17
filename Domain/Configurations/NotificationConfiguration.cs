using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(m => m.Title).IsRequired().HasMaxLength(120);
            builder.Property(m => m.Message).IsRequired().HasMaxLength(400);
            builder.Property(m => m.LinkUrl).HasMaxLength(200);
            builder.Property(m => m.DedupeKey).HasMaxLength(100);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Siyahı: istifadəçinin bildirişləri, oxunmamışlar və ən yenilər əvvəldə
            builder.HasIndex(m => new { m.UserId, m.IsRead, m.CreatedAt });

            // Eyni hadisə üçün eyni istifadəçiyə ikinci bildiriş yazılmır
            builder.HasIndex(m => new { m.UserId, m.DedupeKey })
                   .IsUnique()
                   .HasFilter("[DedupeKey] IS NOT NULL");

            // İstifadəçi silinsə bildirişləri də silinir (hesabla birlikdə gedən məlumatdır)
            builder.HasOne(m => m.User)
                   .WithMany()
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
