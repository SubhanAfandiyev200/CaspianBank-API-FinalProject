using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class OwnBankConfiguration : IEntityTypeConfiguration<OwnBank>
    {
        public void Configure(EntityTypeBuilder<OwnBank> builder)
        {
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.LogoUrl).HasMaxLength(200);
            builder.Property(m => m.Bin).IsRequired().HasMaxLength(8);

            builder.Property(m => m.MinLoanAmount).HasPrecision(18, 2);
            builder.Property(m => m.MaxLoanAmount).HasPrecision(18, 2);
            builder.Property(m => m.AnnualInterestRate).HasPrecision(5, 2);
            builder.Property(m => m.FixedPenalty).HasPrecision(18, 2);
            builder.Property(m => m.DailyPenaltyPercent).HasPrecision(5, 2);
            builder.Property(m => m.MaxPaymentToIncomePercent).HasPrecision(5, 2);
            builder.Property(m => m.InterbankFeePercent).HasPrecision(5, 2);
            builder.Property(m => m.InterbankMinFee).HasPrecision(18, 2);
            builder.Property(m => m.MaxInterbankAmount).HasPrecision(18, 2);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
