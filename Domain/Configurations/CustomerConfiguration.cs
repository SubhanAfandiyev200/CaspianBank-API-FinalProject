using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.Property(m => m.Fin).IsRequired().HasMaxLength(7);
            builder.Property(m => m.IdCardNumber).IsRequired().HasMaxLength(11);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Surname).IsRequired().HasMaxLength(100);
            builder.Property(m => m.FatherName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.MotherName).IsRequired().HasMaxLength(100);
            builder.Property(m => m.BirthPlace).IsRequired().HasMaxLength(100);
            builder.Property(m => m.ResidenceAddress).IsRequired().HasMaxLength(300);
            builder.Property(m => m.BlacklistReason).HasMaxLength(300);
            builder.Property(m => m.PhotoUrl).HasMaxLength(200);

            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Qeydiyyatda FİN və vəsiqə nömrəsi ilə vətəndaş tapılır; ikisi də unikaldır
            builder.HasIndex(m => m.Fin).IsUnique();
            builder.HasIndex(m => m.IdCardNumber).IsUnique();
        }
    }
}
