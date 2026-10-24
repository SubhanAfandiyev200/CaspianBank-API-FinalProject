using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ExternalCardConfiguration : IEntityTypeConfiguration<ExternalCard>
    {
        public void Configure(EntityTypeBuilder<ExternalCard> builder)
        {
            builder.Property(m => m.CardNumber).IsRequired().HasMaxLength(19);
            builder.Property(m => m.HolderName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Nömrə ilə axtarılır və unikaldır
            builder.HasIndex(m => m.CardNumber).IsUnique();

            builder.HasOne(m => m.ExternalBank).WithMany().HasForeignKey(m => m.ExternalBankId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class ExternalTransferConfiguration : IEntityTypeConfiguration<ExternalTransfer>
    {
        public void Configure(EntityTypeBuilder<ExternalTransfer> builder)
        {
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(30);
            builder.Property(m => m.BankName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.CardLast4).IsRequired().HasMaxLength(4);
            builder.Property(m => m.HolderMasked).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Note).HasMaxLength(200);
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.Commission).HasPrecision(18, 2);
            builder.Property(m => m.SourceBalanceAfter).HasPrecision(18, 2);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Eyni sorğu (RequestId) ikinci dəfə gəlsə köçürmə təkrar olunmur: Reference unikaldır
            builder.HasIndex(m => m.Reference).IsUnique();
            builder.HasIndex(m => new { m.UserId, m.CreatedAt });

            // Köçürmə tarixçəsi qalmalıdır: istifadəçi, kart və xarici kart silinəndə qeyd silinmir
            builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.SourceCard).WithMany().HasForeignKey(m => m.SourceCardId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.ExternalCard).WithMany().HasForeignKey(m => m.ExternalCardId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
