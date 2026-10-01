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
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Image).IsRequired().HasMaxLength(300);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
