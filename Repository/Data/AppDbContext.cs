using Domain.Configurations;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<About> Abouts { get; set; }
        public DbSet<CardDesign> CardDesigns { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardTierConfig> CardTierConfigs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<CardHero> CardHeroes { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<ServiceItem> ServiceItems { get; set; }
        public DbSet<ServiceSection> ServiceSections { get; set; }
        public DbSet<EmailOtp> EmailOtps { get; set; }
        public DbSet<AboutPillar> AboutPillars { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<BenefitItem> BenefitItems { get; set; }
        public DbSet<BenefitSection> BenefitSections { get; set; }
        public DbSet<HomeTicker> HomeTickers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Workplace> Workplaces { get; set; }
        public DbSet<Employment> Employments { get; set; }
        public DbSet<ExternalBank> ExternalBanks { get; set; }
        public DbSet<ExternalCard> ExternalCards { get; set; }
        public DbSet<ExternalTransfer> ExternalTransfers { get; set; }
        public DbSet<ExternalLoan> ExternalLoans { get; set; }
        public DbSet<OwnBank> OwnBanks { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanInstallment> LoanInstallments { get; set; }
        public DbSet<LoanPaymentRecord> LoanPaymentRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BillProvider> BillProviders { get; set; }
        public DbSet<BillSubscriber> BillSubscribers { get; set; }
        public DbSet<BillCharge> BillCharges { get; set; }
        public DbSet<BillPayment> BillPayments { get; set; }
        public DbSet<RecurringPayment> RecurringPayments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity-nin öz konfiqurasiyası üçün vacibdir
            builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }
    }
}