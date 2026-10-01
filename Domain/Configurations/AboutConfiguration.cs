using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Configurations
{
    public class AboutConfiguration : IEntityTypeConfiguration<About>
    {
        public void Configure(EntityTypeBuilder<About> builder)
        {
            builder.Property(m => m.Label).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(500);
            builder.Property(m => m.VideoPath).IsRequired();

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        }
    }
}
