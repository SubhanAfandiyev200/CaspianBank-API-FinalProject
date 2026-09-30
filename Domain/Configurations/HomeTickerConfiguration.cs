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
    public class HomeTickerConfiguration : IEntityTypeConfiguration<HomeTicker>
    {
        public void Configure(EntityTypeBuilder<HomeTicker> builder)
        {
            builder.Property(m => m.Text).IsRequired().HasMaxLength(100);

            // SQL-dən əl ilə sətir əlavə edəndə CreatedAt özü dolsun
            builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
