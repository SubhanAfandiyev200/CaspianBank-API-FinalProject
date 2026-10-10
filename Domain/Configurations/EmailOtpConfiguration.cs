using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
    {
        public void Configure(EntityTypeBuilder<EmailOtp> builder)
        {
            builder.Property(m => m.Email).IsRequired().HasMaxLength(256);
            builder.Property(m => m.Purpose).IsRequired().HasMaxLength(50).HasDefaultValue("Register");
            builder.Property(m => m.CodeHash).IsRequired().HasMaxLength(100);
            builder.Property(m => m.VerificationTokenHash).HasMaxLength(100);

            builder.HasIndex(m => m.Email);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
