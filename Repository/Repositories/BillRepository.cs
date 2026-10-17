using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class BillRepository : IBillRepository
    {
        private const int MaxAttempts = 8;

        private readonly AppDbContext _dbContext;

        public BillRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---------- Təchizatçılar ----------

        public async Task<IReadOnlyList<BillProvider>> GetProvidersAsync(bool onlyActive)
        {
            var query = _dbContext.BillProviders.AsNoTracking();
            if (onlyActive)
            {
                query = query.Where(p => p.IsActive);
            }
            return await query.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        }

        public async Task<BillProvider?> GetProviderAsync(int id)
        {
            return await _dbContext.BillProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        }

        // ---------- Abunəçilər ----------

        public async Task<BillSubscriber?> FindSubscriberAsync(int providerId, string identifier)
        {
            return await _dbContext.BillSubscribers
                .AsNoTracking()
                .Include(s => s.Provider)
                .FirstOrDefaultAsync(s => s.ProviderId == providerId && s.Identifier == identifier && s.IsActive);
        }

        public async Task<BillCharge?> GetLastChargeAsync(int subscriberId)
        {
            return await _dbContext.BillCharges
                .AsNoTracking()
                .Where(c => c.SubscriberId == subscriberId)
                .OrderByDescending(c => c.Period)
                .FirstOrDefaultAsync();
        }

        // ---------- Ödəniş ----------

        public async Task<BillResult> PayAsync(BillPaymentPlan plan)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Hər şey bu tranzaksiyada təzədən oxunur: servisdən gələn rəqəmlərə güvənmirik
                var provider = await _dbContext.BillProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plan.ProviderId && p.IsActive);
                if (provider is null)
                {
                    return BillResult.Of(BillOutcome.ProviderNotFound);
                }

                var subscriber = await _dbContext.BillSubscribers
                    .FirstOrDefaultAsync(s => s.ProviderId == plan.ProviderId && s.Identifier == plan.Identifier && s.IsActive);
                if (subscriber is null)
                {
                    return BillResult.Of(BillOutcome.SubscriberNotFound);
                }

                if (plan.Amount < provider.MinPayment || plan.Amount > provider.MaxPayment)
                {
                    return BillResult.Of(BillOutcome.AmountOutOfRange,
                        "The amount must be between " + provider.MinPayment.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        + " and " + provider.MaxPayment.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " AZN.");
                }

                // Ödəniş ödəyənin öz kartından çıxır. Cashback kartı da ödəyə bilər (yalnız yığılan cashback ondadır)
                var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == plan.CardId && c.UserId == plan.UserId);
                if (card is null)
                {
                    return BillResult.Of(BillOutcome.CardNotFound);
                }
                if (card.IsBlocked)
                {
                    return BillResult.Of(BillOutcome.CardBlocked);
                }
                if (card.Balance < plan.Amount)
                {
                    return BillResult.Of(BillOutcome.InsufficientFunds);
                }

                // Əvvəl borc bağlanır, artığı abunəçinin balansında qalır
                var debtBefore = Math.Max(0m, -subscriber.Balance);
                var cleared = Math.Min(plan.Amount, debtBefore);
                var credit = plan.Amount - cleared;
                subscriber.Balance += plan.Amount;
                card.Balance -= plan.Amount;

                var description = provider.Name + " · " + DescribeIdentifier(provider, subscriber.Identifier);
                _dbContext.Transactions.Add(new Transaction
                {
                    CardId = card.Id,
                    Type = TransactionType.BillPayment,
                    IsIncome = false,
                    Amount = plan.Amount,
                    BalanceAfter = card.Balance,
                    Description = description,
                    Reference = plan.Reference
                });

                // Cashback: ödəyən kartın növünün faizi, yalnız Cashback kartı varsa və bloklanmayıbsa
                var cashback = 0m;
                var tier = await _dbContext.CardTierConfigs.AsNoTracking().FirstOrDefaultAsync(t => t.Tier == card.Tier);
                if (tier is not null && tier.CashbackPercent > 0m)
                {
                    var cashbackCard = await _dbContext.Cards.FirstOrDefaultAsync(c => c.UserId == plan.UserId && c.Tier == CardTier.Cashback && !c.IsBlocked);
                    var earned = BillingEngine.Round(plan.Amount * tier.CashbackPercent / 100m);
                    if (cashbackCard is not null && earned > 0m)
                    {
                        cashbackCard.Balance += earned;
                        cashback = earned;
                        _dbContext.Transactions.Add(new Transaction
                        {
                            CardId = cashbackCard.Id,
                            Type = TransactionType.Cashback,
                            IsIncome = true,
                            Amount = earned,
                            BalanceAfter = cashbackCard.Balance,
                            Description = "Cashback · " + provider.Name,
                            Reference = "CB-" + plan.Reference.Substring(3)
                        });
                    }
                }

                _dbContext.BillPayments.Add(new BillPayment
                {
                    UserId = plan.UserId,
                    ProviderId = provider.Id,
                    SubscriberId = subscriber.Id,
                    CardId = card.Id,
                    Amount = plan.Amount,
                    DebtCleared = cleared,
                    CreditAdded = credit,
                    CashbackEarned = cashback,
                    Reference = plan.Reference,
                    ProviderName = provider.Name,
                    Identifier = subscriber.Identifier,
                    SubscriberName = subscriber.FullName
                });

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new BillResult
                    {
                        Outcome = BillOutcome.Success,
                        Reference = plan.Reference,
                        ProviderId = provider.Id,
                        ProviderName = provider.Name,
                        Category = provider.Category,
                        LogoUrl = provider.LogoUrl,
                        Identifier = subscriber.Identifier,
                        SubscriberName = subscriber.FullName,
                        Amount = plan.Amount,
                        DebtCleared = cleared,
                        CreditAdded = credit,
                        CashbackEarned = cashback,
                        CardNumber = card.CardNumber,
                        CardBalanceAfter = card.Balance,
                        SubscriberBalanceAfter = subscriber.Balance,
                        PaidAt = plan.UtcNow
                    };
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();   // növbəti cəhd yenidən oxuyub təkrar yoxlayır
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
                catch (DbUpdateException)
                {
                    // Reference unikal indeksi: bu ödəniş artıq yazılıb
                    await transaction.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    return BillResult.Of(BillOutcome.Duplicate);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return BillResult.Of(BillOutcome.Conflict);
        }

        public async Task<BillResult?> FindPaymentAsync(string reference, string userId)
        {
            var payment = await _dbContext.BillPayments
                .AsNoTracking()
                .Include(p => p.Provider)
                .Include(p => p.Subscriber)
                .Include(p => p.Card)
                .FirstOrDefaultAsync(p => p.Reference == reference && p.UserId == userId);
            if (payment is null)
            {
                return null;
            }

            var row = await _dbContext.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Reference == reference && t.Type == TransactionType.BillPayment && t.CardId == payment.CardId);

            return new BillResult
            {
                Outcome = BillOutcome.Success,
                Reference = payment.Reference,
                ProviderId = payment.ProviderId,
                ProviderName = payment.ProviderName,
                Category = payment.Provider.Category,
                LogoUrl = payment.Provider.LogoUrl,
                Identifier = payment.Identifier,
                SubscriberName = payment.SubscriberName,
                Amount = payment.Amount,
                DebtCleared = payment.DebtCleared,
                CreditAdded = payment.CreditAdded,
                CashbackEarned = payment.CashbackEarned,
                CardNumber = payment.Card.CardNumber,
                CardBalanceAfter = row?.BalanceAfter ?? payment.Card.Balance,
                SubscriberBalanceAfter = payment.Subscriber.Balance,
                PaidAt = payment.CreatedAt
            };
        }

        public async Task<IReadOnlyList<BillPayment>> GetRecentPaymentsAsync(string userId, int take)
        {
            return await _dbContext.BillPayments
                .AsNoTracking()
                .Include(p => p.Provider)
                .Include(p => p.Card)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<(decimal Paid, int Count, decimal Cashback)> GetSummaryAsync(string userId, DateTime fromUtc)
        {
            var rows = await _dbContext.BillPayments
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.CreatedAt >= fromUtc)
                .Select(p => new { p.Amount, p.CashbackEarned })
                .ToListAsync();
            return (rows.Sum(r => r.Amount), rows.Count, rows.Sum(r => r.CashbackEarned));
        }

        // ---------- Gündəlik iş ----------

        public async Task<int> RunMonthlyBillingAsync(DateTime utcNow)
        {
            var period = BillingEngine.PeriodOf(Domain.Loans.LoanEngine.BakuToday(utcNow));
            _dbContext.ChangeTracker.Clear();

            // Bu ay hesabı hələ yazılmamış abunəçilər (mobil nömrələrə aylıq hesab yoxdur)
            var ids = await _dbContext.BillSubscribers
                .AsNoTracking()
                .Where(s => s.IsActive
                         && s.Provider.IsActive
                         && s.Provider.Category != BillCategory.Mobile
                         && !s.Charges.Any(c => c.Period == period))
                .Select(s => s.Id)
                .ToListAsync();

            var written = 0;
            foreach (var id in ids)
            {
                try
                {
                    _dbContext.ChangeTracker.Clear();
                    var subscriber = await _dbContext.BillSubscribers.Include(s => s.Provider).FirstOrDefaultAsync(s => s.Id == id);
                    if (subscriber is null)
                    {
                        continue;
                    }

                    var charge = BillingEngine.ComputeCharge(subscriber.Provider, subscriber, period);
                    if (charge.Amount <= 0m)
                    {
                        continue;
                    }

                    // Pul çatmasa balans mənfiyə düşür, yəni borc yaranır
                    subscriber.Balance -= charge.Amount;
                    _dbContext.BillCharges.Add(new BillCharge
                    {
                        SubscriberId = subscriber.Id,
                        Period = period,
                        Amount = charge.Amount,
                        Usage = charge.Usage,
                        Description = charge.Description
                    });
                    await _dbContext.SaveChangesAsync();
                    written++;
                }
                catch (DbUpdateException)
                {
                    // Eyni anda müştəri ödəniş edib (RowVersion) və ya hesab artıq yazılıb: növbəti işə düşmədə təkrar baxılır
                    _dbContext.ChangeTracker.Clear();
                }
            }

            _dbContext.ChangeTracker.Clear();
            return written;
        }

        // ---------- Köməkçilər ----------

        // Mobil: "055 123 45 67", qalanı: "subscriber 2610458317"
        private static string DescribeIdentifier(BillProvider provider, string identifier)
        {
            if (provider.Category == BillCategory.Mobile && identifier.Length == 10)
            {
                return BillingEngine.FormatPhone(identifier);
            }
            return "subscriber " + identifier;
        }
    }
}
