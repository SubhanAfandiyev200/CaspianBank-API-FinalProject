using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.BalanceAfter).HasPrecision(18, 2);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Reference).IsRequired().HasMaxLength(30);
            builder.Property(m => m.Note).HasMaxLength(140);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Eyni əməliyyat (Reference) eyni növlə iki dəfə yazıla bilməz: formanın iki dəfə göndərilməsi pulu iki dəfə köçürməsin
            builder.HasIndex(m => new { m.Reference, m.Type }).IsUnique();

            // Kartın tarixçəsi (tarixə görə) tez gəlsin
            builder.HasIndex(m => new { m.CardId, m.CreatedAt });

            builder.HasOne(m => m.Card)
                   .WithMany()
                   .HasForeignKey(m => m.CardId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
