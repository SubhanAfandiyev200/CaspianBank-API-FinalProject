using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
    {
        public void Configure(EntityTypeBuilder<Translation> builder)
        {
            builder.Property(m => m.Language).IsRequired().HasMaxLength(5);
            builder.Property(m => m.Key).IsRequired().HasMaxLength(400);
            builder.Property(m => m.Value).IsRequired().HasMaxLength(2000);
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(m => new { m.Language, m.Key }).IsUnique();
        }
    }
}
