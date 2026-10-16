using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class LoanInstallmentConfiguration : IEntityTypeConfiguration<LoanInstallment>
    {
        public void Configure(EntityTypeBuilder<LoanInstallment> builder)
        {
            builder.Property(m => m.PrincipalAmount).HasPrecision(18, 2);
            builder.Property(m => m.InterestAmount).HasPrecision(18, 2);
            builder.Property(m => m.PaidAmount).HasPrecision(18, 2);
            builder.Property(m => m.FixedPenaltyAmount).HasPrecision(18, 2);
            builder.Property(m => m.AccruedPenalty).HasPrecision(18, 2);
            builder.Property(m => m.PaidPenalty).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Bir kreditdə hər sıra nömrəsi bir dəfə olur
            builder.HasIndex(m => new { m.LoanId, m.Number }).IsUnique();

            builder.HasOne(m => m.Loan)
                   .WithMany(l => l.Installments)
                   .HasForeignKey(m => m.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
