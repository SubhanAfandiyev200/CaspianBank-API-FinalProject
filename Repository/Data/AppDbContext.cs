using Domain.Configurations;
using Domain.Entities;
using Domain.Enums;
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
        public DbSet<DepositPlan> DepositPlans { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
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
        public DbSet<TreasuryEntry> TreasuryEntries { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Bankın xəzinəsinin jurnalı hər pul əməliyyatı ilə EYNI saxlamada yazılır: ya əməliyyat və xəzinə sətri birlikdə olur, ya heç biri.
        // Bunun üçün əməliyyatları yazan hər yerə toxunmaq lazım gəlmir
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddTreasuryEntries();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddTreasuryEntries();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AddTreasuryEntries()
        {
            var entries = new List<TreasuryEntry>();

            foreach (var entry in ChangeTracker.Entries<Transaction>().Where(e => e.State == EntityState.Added))
            {
                var row = entry.Entity;
                switch (row.Type)
                {
                    case TransactionType.Commission:
                        entries.Add(NewTreasury(TreasuryEntryKind.Commission, row.Amount, "Transfer commission", row.Reference));
                        break;
                    case TransactionType.CardFee:
                        entries.Add(NewTreasury(TreasuryEntryKind.CardFee, row.Amount, "Card opening fee", row.Reference));
                        break;
                    case TransactionType.Cashback:
                        entries.Add(NewTreasury(TreasuryEntryKind.Cashback, -row.Amount, "Cashback paid to a customer", row.Reference));
                        break;
                    case TransactionType.LoanDisbursement:
                        entries.Add(NewTreasury(TreasuryEntryKind.LoanPaidOut, -row.Amount, "Loan paid out", row.Reference));
                        break;
                    case TransactionType.DepositOpen:
                        entries.Add(NewTreasury(TreasuryEntryKind.DepositReceived, row.Amount, "Deposit received", row.Reference));
                        break;
                    case TransactionType.DepositPayout:
                        entries.Add(NewTreasury(TreasuryEntryKind.DepositReturned, -row.Amount, "Deposit returned to a customer", row.Reference));
                        break;
                    case TransactionType.DepositInterest:
                        entries.Add(NewTreasury(TreasuryEntryKind.DepositInterest, -row.Amount, "Deposit interest paid", row.Reference));
                        break;
                }
            }

            // Kredit ödənişi bir neçə aylıq ödənişi örtə bilər: eyni kodlu qeydlər birləşdirilir (əsas borc, faiz və cərimə ayrıca)
            var payments = ChangeTracker.Entries<LoanPaymentRecord>().Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
            foreach (var group in payments.GroupBy(p => p.TransactionReference))
            {
                var principal = group.Sum(p => p.PrincipalPart);
                var interest = group.Sum(p => p.InterestPart);
                var penalty = group.Sum(p => p.PenaltyPart);
                if (principal > 0m)
                {
                    entries.Add(NewTreasury(TreasuryEntryKind.LoanPrincipal, principal, "Loan principal repaid", group.Key));
                }
                if (interest > 0m)
                {
                    entries.Add(NewTreasury(TreasuryEntryKind.LoanInterest, interest, "Loan interest received", group.Key));
                }
                if (penalty > 0m)
                {
                    entries.Add(NewTreasury(TreasuryEntryKind.LoanPenalty, penalty, "Loan late penalty received", group.Key));
                }
            }

            if (entries.Count > 0)
            {
                TreasuryEntries.AddRange(entries);
            }
        }

        private static TreasuryEntry NewTreasury(TreasuryEntryKind kind, decimal amount, string description, string reference)
        {
            return new TreasuryEntry { Kind = kind, Amount = amount, Description = description, Reference = reference };
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity-nin öz konfiqurasiyası üçün vacibdir
            builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }
    }
}