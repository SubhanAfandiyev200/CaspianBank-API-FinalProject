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
    public class AboutPillarConfiguration : IEntityTypeConfiguration<AboutPillar>
    {
        public void Configure(EntityTypeBuilder<AboutPillar> builder)
        {
            builder.Property(m => m.Title).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Description).IsRequired().HasMaxLength(300);

            builder.Property(m => m.Image).IsRequired().HasMaxLength(300);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
