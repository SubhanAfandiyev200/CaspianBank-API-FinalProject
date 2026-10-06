using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class SettingConfiguration : IEntityTypeConfiguration<Setting>
    {
        public void Configure(EntityTypeBuilder<Setting> builder)
        {
            builder.Property(m => m.Key).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Value).IsRequired().HasMaxLength(500);

            // Eyni açar iki dəfə ola bilməz (GetAll lüğətə çevriləndə toqquşma olmasın)
            builder.HasIndex(m => m.Key).IsUnique();

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
