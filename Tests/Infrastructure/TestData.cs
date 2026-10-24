using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service;
using Service.Helpers;

namespace CaspianBank.Tests.Infrastructure
{
    // Testlər üçün hazır məlumat yaradan köməkçilər. Hər çağırış təkrarsız FİN, email, telefon və kart nömrəsi verir
    public static class TestData
    {
        public const string Password = "Salam123!";

        private static long _counter = 100000;

        public static long Next()
        {
            return Interlocked.Increment(ref _counter);
        }

        public static string UniqueEmail(string prefix = "user")
        {
            return $"{prefix}{Next()}@test.az";
        }

        private static readonly SemaphoreSlim BankLock = new SemaphoreSlim(1, 1);

        // Caspian Bank-ın öz sətri (kredit və köçürmə qaydaları) və xəzinənin başlanğıc kapitalı. Yoxdursa yaradılır
        public static async Task<OwnBank> EnsureOwnBankAsync(AppDbContext db)
        {
            await BankLock.WaitAsync();
            try
            {
                return await EnsureOwnBankCoreAsync(db);
            }
            finally
            {
                BankLock.Release();
            }
        }

        private static async Task<OwnBank> EnsureOwnBankCoreAsync(AppDbContext db)
        {
            var bank = await db.OwnBanks.FirstOrDefaultAsync();
            if (bank is not null)
            {
                return bank;
            }

            bank = new OwnBank
            {
                Name = "Caspian Bank",
                Bin = "96301020",
                MinLoanAmount = 500m,
                MaxLoanAmount = 50000m,
                MinTermMonths = 6,
                MaxTermMonths = 48,
                AnnualInterestRate = 10m,
                MinAge = 18,
                FixedPenalty = 10m,
                DailyPenaltyPercent = 0.5m,
                CourtAfterDays = 90,
                MaxActiveLoans = 3,
                MaxPaymentToIncomePercent = 45m,
                OfferValidDays = 7,
                MinEarlyPayment = 10m,
                EarlyRepaymentMinPercent = 10m,
                EarlyRepaymentRateReduction = 1m,
                MinAnnualInterestRate = 5m,
                MaxInterbankAmount = 10000m,
                OwnFunds = 2400000m,
                OperatingCostMonthly = 150m,
                ProfitTaxPercent = 20m,
                ReservePercent = 10m
            };
            db.OwnBanks.Add(bank);
            db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.Capital, Amount = bank.OwnFunds, Description = "Starting capital", Reference = "CAPITAL" });
            await db.SaveChangesAsync();
            return bank;
        }

        // Reyestrdəki vətəndaş
        public static async Task<Customer> NewCustomerAsync(AppDbContext db, string name = "Test", string surname = "Customer",
                                                            DateTime? birthDate = null, bool deceased = false, bool blacklisted = false)
        {
            var n = Next();
            var customer = new Customer
            {
                Fin = "T" + (n % 1000000).ToString("D6"),
                IdCardNumber = "AZE" + (10000000 + n % 90000000).ToString("D8"),
                Name = name,
                Surname = surname,
                FatherName = "Father",
                MotherName = "Mother",
                BirthDate = birthDate ?? new DateTime(1990, 1, 1),
                BirthPlace = "Baku",
                ResidenceAddress = "Baku",
                DeathDate = deceased ? new DateTime(2020, 1, 1) : null,
                IsBlacklisted = blacklisted
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return customer;
        }

        // Hesab (UserManager ilə, parol Salam123!)
        public static async Task<AppUser> NewUserAsync(IServiceProvider services, Customer? customer = null, string role = Roles.Customer,
                                                       string? email = null, string? phone = null, bool restricted = false, string password = Password)
        {
            var manager = services.GetRequiredService<UserManager<AppUser>>();
            email ??= UniqueEmail();
            var user = new AppUser
            {
                Name = customer?.Name ?? "Test",
                Surname = customer?.Surname ?? "User",
                BirthDay = customer?.BirthDate ?? new DateTime(1990, 1, 1),
                FinKod = customer?.Fin,
                CustomerId = customer?.Id,
                Email = email,
                UserName = email,
                PhoneNumber = phone ?? "05" + (10000000 + Next() % 90000000).ToString("D8"),
                EmailConfirmed = true,
                IsRestricted = restricted
            };

            var created = await manager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException("User not created: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            await manager.AddToRoleAsync(user, role);
            return user;
        }

        // Kart (balansla), CardTierConfig sətrindəki dizaynla
        public static async Task<Card> NewCardAsync(AppDbContext db, string userId, CardTier tier = CardTier.Standard, decimal balance = 0m, bool blocked = false)
        {
            var config = await db.CardTierConfigs.AsNoTracking().FirstAsync(c => c.Tier == tier);
            var card = new Card
            {
                UserId = userId,
                Tier = tier,
                CardDesignId = config.CardDesignId,
                CardNumber = CardNumberGenerator.Generate(),
                ExpiryDate = DateTime.UtcNow.AddYears(4),
                Balance = balance,
                IsBlocked = blocked
            };
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            return card;
        }

        // Müştəri: reyestr sətri + hesab + Standard və Cashback kart
        public static async Task<CustomerKit> NewCustomerKitAsync(IServiceProvider services, decimal standardBalance = 0m, string name = "Test", string surname = "Customer",
                                                                  DateTime? birthDate = null, string? email = null)
        {
            var db = services.GetRequiredService<AppDbContext>();
            await EnsureOwnBankAsync(db);
            var customer = await NewCustomerAsync(db, name, surname, birthDate);
            var user = await NewUserAsync(services, customer, email: email);
            var standard = await NewCardAsync(db, user.Id, CardTier.Standard, standardBalance);
            var cashback = await NewCardAsync(db, user.Id, CardTier.Cashback, 0m);
            db.ChangeTracker.Clear();
            return new CustomerKit(customer, user, standard, cashback);
        }

        // Bazadan təzə balans
        public static async Task<decimal> BalanceAsync(AppDbContext db, int cardId)
        {
            return await db.Cards.AsNoTracking().Where(c => c.Id == cardId).Select(c => c.Balance).FirstAsync();
        }

        public static async Task SetBalanceAsync(AppDbContext db, int cardId, decimal balance)
        {
            await db.Cards.Where(c => c.Id == cardId).ExecuteUpdateAsync(s => s.SetProperty(c => c.Balance, balance));
            db.ChangeTracker.Clear();
        }
    }

    public sealed record CustomerKit(Customer Customer, AppUser User, Card Standard, Card Cashback);
}
