using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class DepositInterestPostingConfiguration : IEntityTypeConfiguration<DepositInterestPosting>
    {
        public void Configure(EntityTypeBuilder<DepositInterestPosting> builder)
        {
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.HasOne(m => m.Deposit).WithMany(d => d.Postings).HasForeignKey(m => m.DepositId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(m => new { m.DepositId, m.Number }).IsUnique();
            builder.HasIndex(m => m.CreatedAt);
        }
    }
}
