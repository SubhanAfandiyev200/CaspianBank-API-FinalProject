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

            // FİN yalnız ilk kart alanda daxil edilir, ona görə NULL-lar unique yoxlamadan kənardır
            builder.HasIndex(u => u.FinKod)
                .IsUnique()
                .HasFilter("[FinKod] IS NOT NULL");
        }
    }
}
