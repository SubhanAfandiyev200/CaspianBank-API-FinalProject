using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.Property(m => m.ContractNumber).IsRequired().HasMaxLength(20);
            builder.Property(m => m.PurposeNote).HasMaxLength(200);
            builder.Property(m => m.RejectReason).HasMaxLength(300);

            builder.Property(m => m.RequestedAmount).HasPrecision(18, 2);
            builder.Property(m => m.ApprovedAmount).HasPrecision(18, 2);
            builder.Property(m => m.FirstApprovedAmount).HasPrecision(18, 2);
            builder.Property(m => m.FirstApprovedByUserId).HasMaxLength(450);
            builder.Property(m => m.MonthlyPayment).HasPrecision(18, 2);
            builder.Property(m => m.TotalRepayment).HasPrecision(18, 2);
            builder.Property(m => m.OutstandingPrincipal).HasPrecision(18, 2);
            builder.Property(m => m.IncomeAtApply).HasPrecision(18, 2);
            builder.Property(m => m.InitialAnnualInterestRate).HasPrecision(5, 2);
            builder.Property(m => m.AnnualInterestRate).HasPrecision(5, 2);
            builder.Property(m => m.FixedPenalty).HasPrecision(18, 2);
            builder.Property(m => m.DailyPenaltyPercent).HasPrecision(5, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(m => m.RowVersion).IsRowVersion();

            // Ödəniş kodu unikaldır: Bills-də kredit bu kodla tapılır
            builder.HasIndex(m => m.ContractNumber).IsUnique();

            // "Mənim kreditlərim" və aktiv kredit sayı sorğuları tez işləsin
            builder.HasIndex(m => new { m.BorrowerUserId, m.Status });

            // Kredit və ona bağlı cədvəl səssizcə silinməsin
            builder.HasOne(m => m.Borrower)
                   .WithMany()
                   .HasForeignKey(m => m.BorrowerUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.OwnBank)
                   .WithMany()
                   .HasForeignKey(m => m.OwnBankId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.TargetCard)
                   .WithMany()
                   .HasForeignKey(m => m.TargetCardId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
