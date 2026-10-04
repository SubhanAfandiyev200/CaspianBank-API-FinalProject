using Domain.Configurations;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<About> Abouts { get; set; }
        public DbSet<ServiceItem> ServiceItems { get; set; }
        public DbSet<ServiceSection> ServiceSections { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }
        public DbSet<AboutPillar> AboutPillars { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<BenefitItem> BenefitItems { get; set; }
        public DbSet<BenefitSection> BenefitSections { get; set; }
        public DbSet<HomeTicker> HomeTickers { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity-nin öz konfiqurasiyası üçün vacibdir
            builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }
    }
}