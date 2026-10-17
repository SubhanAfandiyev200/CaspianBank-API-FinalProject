using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.Data;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Qeydiyyatdan keçmiş 6 sınaq müştərisinin Caspian kredit tarixçəsi (hər vəziyyətdən bir nümunə):
    //   Aysel  KR-2025-000012  Closed      vaxtında ödənilib
    //   Elçin  KR-2025-000041  Active      vaxtında ödənir, hissəvi erkən ödənişlə faiz 10% -> 9%
    //   Nigar  KR-2026-000007  Active      iki ay gecikib (cərimə yazılır, 90 gündən sonra Court)
    //   Rauf   KR-2026-000019  Offered     5000 istəyib, 3000 təklif olunub, cavab gözləyir
    //   Tural  KR-2025-000003  Closed      vaxtından əvvəl tam bağlanıb
    //   Tural  KR-2026-000021  Pending     işçinin baxışını gözləyir
    // Tarixçə LoanEngine ilə (müştərinin edəcəyi eyni qaydalarla) yaradılır, ona görə rəqəmlər real ödənişlərlə eyni çıxır.
    // Kart jurnalına (Transactions) toxunulmur: bunlar keçmiş əməliyyatlardır, kartların başlanğıc balansı artıq verilib.
    // Kredit artıq varsa (ödəniş koduna görə) və ya müştəri/kart tapılmırsa sətir atlanır.
    public static class LoanSeeder
    {
        public static async Task SeedAsync(AppDbContext db, ILogger logger)
        {
            db.ChangeTracker.Clear();

            var bank = await db.OwnBanks.AsNoTracking().FirstOrDefaultAsync();
            if (bank is null)
            {
                logger.LogWarning("LoanSeeder: OwnBank sətri yoxdur (registry.sql işlədilməyib), kreditlər toxumlanmadı.");
                return;
            }

            var accountant = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "accountant@caspianbank.az");
            var reviewerId = accountant?.Id;

            await ClosedOnTimeAsync(db, bank, logger, reviewerId);
            await ActiveWithEarlyPaymentAsync(db, bank, logger, reviewerId);
            await ActiveWithDelaysAsync(db, bank, logger, reviewerId);
            await OfferedAsync(db, bank, logger, reviewerId);
            await ClosedEarlyAsync(db, bank, logger, reviewerId);
            await PendingAsync(db, bank, logger);
        }

        // ---------- Köməkçilər ----------

        // FİN-ə görə qeydiyyatlı müştəri və onun Standard kartı
        private static async Task<(AppUser? User, Card? Card)> FindAsync(AppDbContext db, string fin, ILogger logger)
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Fin == fin);
            var user = customer is null ? null : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.CustomerId == customer.Id);
            if (user is null)
            {
                logger.LogWarning("LoanSeeder: {Fin} üçün qeydiyyatlı müştəri yoxdur, kredit atlandı.", fin);
                return (null, null);
            }

            var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id && c.Tier == CardTier.Standard);
            if (card is null)
            {
                logger.LogWarning("LoanSeeder: {Fin} üçün Standard kart yoxdur, kredit atlandı.", fin);
                return (null, null);
            }

            return (user, card);
        }

        private static Loan NewLoan(OwnBank bank, AppUser user, Card card, string contract, LoanPurpose purpose, string? note,
                                    decimal requested, int term, decimal income, DateTime created)
        {
            return new Loan
            {
                OwnBankId = bank.Id,
                BorrowerUserId = user.Id,
                Purpose = purpose,
                PurposeNote = note,
                ContractNumber = contract,
                RequestedAmount = requested,
                TermMonths = term,
                IncomeAtApply = income,
                InitialAnnualInterestRate = bank.AnnualInterestRate,
                AnnualInterestRate = bank.AnnualInterestRate,
                FixedPenalty = bank.FixedPenalty,
                DailyPenaltyPercent = bank.DailyPenaltyPercent,
                TargetCardId = card.Id,
                Status = LoanStatus.Pending,
                CreatedAt = created
            };
        }

        // Təsdiq olunmuş kreditin pulu (keçmiş tarixdə) verilir: aylıq ödənişlər yaranır və bazaya yazılır (Id-lər lazımdır)
        private static async Task<Loan> DisburseAsync(AppDbContext db, Loan loan, decimal approved, DateTime disbursedAt, string? reviewerId)
        {
            loan.ApprovedAmount = approved;
            loan.ReviewedByUserId = reviewerId;
            loan.ReviewedAt = disbursedAt.AddHours(-20);
            LoanEngine.Activate(loan, disbursedAt);
            db.Loans.Add(loan);
            await db.SaveChangesAsync();
            return loan;
        }

        // Aylıq ödənişin son günü səhər saat 9-da ödənilir (hesab günü = son gün)
        private static DateTime PayTime(DateTime dueDate)
        {
            return dueDate.Date.AddHours(9);
        }

        private static async Task PayOnDueDateAsync(AppDbContext db, Loan loan, int number, string cardNumber)
        {
            var installment = loan.Installments.First(i => i.Number == number);
            var asOf = PayTime(installment.DueDate);
            var due = LoanEngine.GetDue(loan, asOf);
            var records = LoanEngine.ApplyInstallmentPayment(loan, due, asOf, cardNumber, "SEED-" + loan.ContractNumber.Substring(3) + "-" + number);
            db.LoanPaymentRecords.AddRange(records);
            await db.SaveChangesAsync();
        }

        private static async Task<bool> ExistsAsync(AppDbContext db, string contract)
        {
            return await db.Loans.AsNoTracking().AnyAsync(l => l.ContractNumber == contract);
        }

        // ---------- Nümunələr ----------

        // Aysel: 6000 AZN, 12 ay, bütün ödənişlər vaxtında; kredit 2026-01-31-də bağlanıb
        private static async Task ClosedOnTimeAsync(AppDbContext db, OwnBank bank, ILogger logger, string? reviewerId)
        {
            const string contract = "KR-2025-000012";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "5KM2R9A", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2025, 1, 19, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Consumer, null, 6000m, 12, 2400m, created);
            await DisburseAsync(db, loan, 6000m, new DateTime(2025, 1, 20, 8, 0, 0, DateTimeKind.Utc), reviewerId);

            for (var n = 1; n <= 12; n++)
            {
                await PayOnDueDateAsync(db, loan, n, card.CardNumber);
            }
            db.ChangeTracker.Clear();
        }

        // Elçin: 8000 AZN, 24 ay. Dekabr-may ödənişləri vaxtında, 2026-06-15-də 1000 AZN hissəvi erkən ödəniş (faiz 10% -> 9%), sonra iyun-sentyabr vaxtında
        private static async Task ActiveWithEarlyPaymentAsync(AppDbContext db, OwnBank bank, ILogger logger, string? reviewerId)
        {
            const string contract = "KR-2025-000041";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "7XQ4D1B", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2025, 11, 19, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Car, null, 8000m, 24, 1900m, created);
            await DisburseAsync(db, loan, 8000m, new DateTime(2025, 11, 20, 8, 0, 0, DateTimeKind.Utc), reviewerId);

            for (var n = 1; n <= 6; n++)
            {
                await PayOnDueDateAsync(db, loan, n, card.CardNumber);
            }

            var earlyAt = new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc);
            var problem = LoanEngine.CheckPartialEarly(loan, 1000m, earlyAt, bank);
            if (problem is not null)
            {
                logger.LogWarning("LoanSeeder: {Contract} hissəvi ödəniş edilmədi: {Problem}", contract, problem);
            }
            else
            {
                var record = LoanEngine.ApplyPartialEarly(loan, 1000m, earlyAt, card.CardNumber, "SEED-" + contract.Substring(3) + "-EP", bank);
                db.LoanPaymentRecords.Add(record);
                await db.SaveChangesAsync();
            }

            for (var n = 7; n <= 10; n++)
            {
                await PayOnDueDateAsync(db, loan, n, card.CardNumber);
            }
            db.ChangeTracker.Clear();
        }

        // Nigar: 3000 AZN, 12 ay. Fevral-iyul vaxtında; avqust və sentyabr ödənilməyib (cərimə yazılır)
        private static async Task ActiveWithDelaysAsync(AppDbContext db, OwnBank bank, ILogger logger, string? reviewerId)
        {
            const string contract = "KR-2026-000007";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "3HT8V6C", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2026, 1, 19, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Repair, null, 3000m, 12, 1100m, created);
            await DisburseAsync(db, loan, 3000m, new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc), reviewerId);

            for (var n = 1; n <= 6; n++)
            {
                await PayOnDueDateAsync(db, loan, n, card.CardNumber);
            }

            // Gecikmiş aylar üçün bu günə qədər cərimə yazılır (gündəlik iş də eyni işi görür)
            LoanEngine.AccrueAll(loan, LoanEngine.BakuToday(DateTime.UtcNow));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        // Rauf: 5000 AZN istəyib, maaşı və başqa bankdakı gecikmələrə görə işçi 3000 AZN təklif edib; müştərinin cavabı gözlənilir
        private static async Task OfferedAsync(AppDbContext db, OwnBank bank, ILogger logger, string? reviewerId)
        {
            const string contract = "KR-2026-000019";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "9PL2N5D", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Education, null, 5000m, 24, 750m, created);

            var reviewedAt = new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);
            var quote = LoanEngine.Quote(3000m, loan.AnnualInterestRate, loan.TermMonths);
            loan.ApprovedAmount = 3000m;
            loan.MonthlyPayment = quote.MonthlyPayment;
            loan.TotalRepayment = quote.TotalRepayment;
            loan.ReviewedByUserId = reviewerId;
            loan.ReviewedAt = reviewedAt;
            loan.OfferExpiresAt = reviewedAt.AddDays(bank.OfferValidDays);
            loan.Status = LoanStatus.Offered;

            db.Loans.Add(loan);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        // Tural: 2000 AZN, 6 ay; aprel və may vaxtında, 2025-06-20-də krediti vaxtından əvvəl tam bağlayıb
        private static async Task ClosedEarlyAsync(AppDbContext db, OwnBank bank, ILogger logger, string? reviewerId)
        {
            const string contract = "KR-2025-000003";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "6FG3J8H", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2025, 3, 9, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Consumer, null, 2000m, 6, 1500m, created);
            await DisburseAsync(db, loan, 2000m, new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc), reviewerId);

            await PayOnDueDateAsync(db, loan, 1, card.CardNumber);
            await PayOnDueDateAsync(db, loan, 2, card.CardNumber);

            var payoffAt = new DateTime(2025, 6, 20, 9, 0, 0, DateTimeKind.Utc);
            var records = LoanEngine.ApplyPayoff(loan, payoffAt, card.CardNumber, "SEED-" + contract.Substring(3) + "-PO");
            db.LoanPaymentRecords.AddRange(records);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        // Tural: 4000 AZN, 18 ay, işçinin baxışını gözləyir
        private static async Task PendingAsync(AppDbContext db, OwnBank bank, ILogger logger)
        {
            const string contract = "KR-2026-000021";
            if (await ExistsAsync(db, contract))
            {
                return;
            }
            var (user, card) = await FindAsync(db, "6FG3J8H", logger);
            if (user is null || card is null)
            {
                return;
            }

            var created = new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc);
            var loan = NewLoan(bank, user, card, contract, LoanPurpose.Business, null, 4000m, 18, 1500m, created);
            var quote = LoanEngine.Quote(4000m, loan.AnnualInterestRate, loan.TermMonths);
            loan.MonthlyPayment = quote.MonthlyPayment;
            loan.TotalRepayment = quote.TotalRepayment;

            db.Loans.Add(loan);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }
}
