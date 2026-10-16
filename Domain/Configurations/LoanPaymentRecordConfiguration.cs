using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class LoanPaymentRecordConfiguration : IEntityTypeConfiguration<LoanPaymentRecord>
    {
        public void Configure(EntityTypeBuilder<LoanPaymentRecord> builder)
        {
            builder.Property(m => m.Amount).HasPrecision(18, 2);
            builder.Property(m => m.PenaltyPart).HasPrecision(18, 2);
            builder.Property(m => m.InterestPart).HasPrecision(18, 2);
            builder.Property(m => m.PrincipalPart).HasPrecision(18, 2);
            builder.Property(m => m.InterestRateAfter).HasPrecision(5, 2);
            builder.Property(m => m.CardNumber).IsRequired().HasMaxLength(16);
            builder.Property(m => m.TransactionReference).IsRequired().HasMaxLength(30);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.LoanId);
            builder.HasIndex(m => m.TransactionReference);

            builder.HasOne(m => m.Loan)
                   .WithMany(l => l.PaymentRecords)
                   .HasForeignKey(m => m.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Aylıq ödəniş silinsə ödəniş tarixçəsi qalmalıdır
            builder.HasOne<LoanInstallment>()
                   .WithMany()
                   .HasForeignKey(m => m.InstallmentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
