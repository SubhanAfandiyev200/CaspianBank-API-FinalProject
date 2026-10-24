using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.Property(m => m.Subject).IsRequired().HasMaxLength(120);
            builder.Property(m => m.GuestEmail).HasMaxLength(200);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Müştərinin siyahısı və işçinin siyahısı (vəziyyətə görə, ən yeni fəaliyyət əvvəldə)
            builder.HasIndex(m => new { m.UserId, m.LastActivityAt });
            builder.HasIndex(m => new { m.Status, m.LastActivityAt });

            // İstifadəçi silinsə yazışması da silinir (hesabla gedən məlumatdır)
            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
    {
        public void Configure(EntityTypeBuilder<SupportMessage> builder)
        {
            builder.Property(m => m.AuthorUserId).IsRequired().HasMaxLength(450);
            builder.Property(m => m.AuthorName).IsRequired().HasMaxLength(120);
            builder.Property(m => m.Body).IsRequired().HasMaxLength(2000);
            builder.Property(m => m.ImagePath).HasMaxLength(300);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => new { m.TicketId, m.CreatedAt });

            builder.HasOne(m => m.Ticket).WithMany(t => t.Messages).HasForeignKey(m => m.TicketId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
