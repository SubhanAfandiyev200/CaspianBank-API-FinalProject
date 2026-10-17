using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Surname)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.FinKod)
                .HasMaxLength(10);

            builder.Property(u => u.RestrictionReason).HasMaxLength(300);
            builder.Property(u => u.RestrictedBy).HasMaxLength(100);

            // FİN yalnız ilk kart alanda daxil edilir, ona görə NULL-lar unique yoxlamadan kənardır
            builder.HasIndex(u => u.FinKod)
                .IsUnique()
                .HasFilter("[FinKod] IS NOT NULL");

            // Bir vətəndaş yalnız bir hesaba bağlana bilər (işçilərdə CustomerId boşdur)
            builder.HasIndex(u => u.CustomerId)
                .IsUnique()
                .HasFilter("[CustomerId] IS NOT NULL");

            builder.HasOne(u => u.Customer)
                .WithMany()
                .HasForeignKey(u => u.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
