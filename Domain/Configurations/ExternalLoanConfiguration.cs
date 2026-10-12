using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ExternalLoanConfiguration : IEntityTypeConfiguration<ExternalLoan>
    {
        public void Configure(EntityTypeBuilder<ExternalLoan> builder)
        {
            builder.Property(m => m.PurposeNote).HasMaxLength(200);
            builder.Property(m => m.Note).HasMaxLength(300);
            builder.Property(m => m.BorrowedAmount).HasPrecision(18, 2);
            builder.Property(m => m.PaidAmount).HasPrecision(18, 2);
            builder.Property(m => m.MonthlyPayment).HasPrecision(18, 2);
            builder.Property(m => m.OverdueAmount).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => m.CustomerId);

            builder.HasOne(m => m.Customer)
                   .WithMany(c => c.ExternalLoans)
                   .HasForeignKey(m => m.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.ExternalBank)
                   .WithMany(b => b.ExternalLoans)
                   .HasForeignKey(m => m.ExternalBankId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
