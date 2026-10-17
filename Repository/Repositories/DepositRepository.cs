using Domain.Deposits;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;
using System.Globalization;

namespace Repository.Repositories
{
    public class DepositRepository : IDepositRepository
    {
        private const int MaxAttempts = 8;
        private const int MaturityBatch = 200;

        private readonly AppDbContext _dbContext;

        public DepositRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<DepositPlan>> GetPlansAsync(bool activeOnly)
        {
            var query = _dbContext.DepositPlans.AsNoTracking();
            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }
            return await query.OrderBy(p => p.TermMonths).ToListAsync();
        }

        public async Task<DepositPlan?> GetPlanAsync(int id)
        {
            return await _dbContext.DepositPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdatePlanAsync(DepositPlan plan)
        {
            _dbContext.DepositPlans.Update(plan);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task<DepositOpenResult> OpenAsync(DepositOpenPlan plan)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Hər şey bu tranzaksiyada təzədən oxunur: servisdən gələn rəqəmlərə güvənmirik
                var depositPlan = await _dbContext.DepositPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == plan.PlanId);
                if (depositPlan is null)
                {
                    return new DepositOpenResult(DepositOutcome.PlanNotFound);
                }
                if (!depositPlan.IsActive)
                {
                    return new DepositOpenResult(DepositOutcome.PlanInactive);
                }
                if (plan.Amount < depositPlan.MinAmount || plan.Amount > depositPlan.MaxAmount)
                {
                    return new DepositOpenResult(DepositOutcome.AmountOutOfRange);
                }

                var source = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == plan.SourceCardId && c.UserId == plan.UserId);
                if (source is null)
                {
                    return new DepositOpenResult(DepositOutcome.SourceNotFound);
                }
                if (source.IsBlocked)
                {
                    return new DepositOpenResult(DepositOutcome.SourceBlocked);
                }

                // Pul qayıdacaq kart: öz kartı, bloklanmamış, Cashback olmayan (Cashback kartı yalnız cashback yığır)
                var payout = await _dbContext.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == plan.PayoutCardId && c.UserId == plan.UserId);
                if (payout is null || payout.IsBlocked || payout.Tier == CardTier.Cashback)
                {
                    return new DepositOpenResult(DepositOutcome.PayoutNotAllowed);
                }

                if (source.Balance < plan.Amount)
                {
                    return new DepositOpenResult(DepositOutcome.InsufficientFunds);
                }

                source.Balance -= plan.Amount;
                _dbContext.Transactions.Add(new Transaction
                {
                    CardId = source.Id,
                    Type = TransactionType.DepositOpen,
                    IsIncome = false,
                    Amount = plan.Amount,
                    BalanceAfter = source.Balance,
                    Description = "Term deposit · " + depositPlan.TermMonths + " months",
                    Reference = plan.Reference
                });

                var deposit = new Deposit
                {
                    UserId = plan.UserId,
                    PlanId = depositPlan.Id,
                    TermMonths = depositPlan.TermMonths,
                    AnnualRate = depositPlan.AnnualRate,
                    Principal = plan.Amount,
                    ExpectedInterest = DepositRules.Interest(plan.Amount, depositPlan.AnnualRate, depositPlan.TermMonths),
                    StartedAt = plan.UtcNow,
                    MaturesAt = DepositRules.MaturityDate(plan.UtcNow, depositPlan.TermMonths),
                    SourceCardId = source.Id,
                    PayoutCardId = payout.Id,
                    Status = DepositStatus.Active,
                    Reference = plan.Reference
                };
                _dbContext.Deposits.Add(deposit);

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new DepositOpenResult(DepositOutcome.Success) { Deposit = deposit, SourceBalanceAfter = source.Balance };
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();   // növbəti cəhd yenidən oxuyub təkrar yoxlayır
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
                catch (DbUpdateException)
                {
                    // Reference unikal indeksi: bu depozit artıq açılıb
                    await transaction.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    return new DepositOpenResult(DepositOutcome.Duplicate);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return new DepositOpenResult(DepositOutcome.Conflict);
        }

        public async Task<List<Deposit>> GetByUserAsync(string userId)
        {
            return await _dbContext.Deposits
                .AsNoTracking()
                .Include(d => d.SourceCard)
                .Include(d => d.PayoutCard)
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.Status)
                .ThenBy(d => d.MaturesAt)
                .ToListAsync();
        }

        public async Task<Deposit?> FindByReferenceAsync(string reference, string userId)
        {
            return await _dbContext.Deposits
                .AsNoTracking()
                .Include(d => d.SourceCard)
                .Include(d => d.PayoutCard)
                .FirstOrDefaultAsync(d => d.Reference == reference && d.UserId == userId);
        }

        public async Task<(List<Deposit> Items, int Total)> GetPagedAsync(string? search, DepositStatus? status, int page, int pageSize)
        {
            var query = _dbContext.Deposits.AsNoTracking().Include(d => d.User).Include(d => d.PayoutCard).AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(d => d.Reference.Contains(term)
                                        || (d.User.Email != null && d.User.Email.Contains(term))
                                        || d.User.Name.Contains(term)
                                        || d.User.Surname.Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(d => d.Status)
                .ThenBy(d => d.MaturesAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<DepositTotals> GetTotalsAsync()
        {
            var active = _dbContext.Deposits.AsNoTracking().Where(d => d.Status == DepositStatus.Active);
            var paidInterest = await _dbContext.Transactions.AsNoTracking()
                .Where(t => t.Type == TransactionType.DepositInterest)
                .SumAsync(t => (decimal?)t.Amount);

            return new DepositTotals
            {
                ActiveCount = await active.CountAsync(),
                ActivePrincipal = await active.SumAsync(d => (decimal?)d.Principal) ?? 0m,
                ExpectedInterest = await active.SumAsync(d => (decimal?)d.ExpectedInterest) ?? 0m,
                PaidInterest = paidInterest ?? 0m
            };
        }

        public async Task<List<MaturedDeposit>> PayMaturedAsync(DateTime utcNow)
        {
            _dbContext.ChangeTracker.Clear();
            var dueIds = await _dbContext.Deposits
                .AsNoTracking()
                .Where(d => d.Status == DepositStatus.Active && d.MaturesAt <= utcNow)
                .OrderBy(d => d.MaturesAt)
                .Select(d => d.Id)
                .Take(MaturityBatch)
                .ToListAsync();

            var paid = new List<MaturedDeposit>();
            foreach (var id in dueIds)
            {
                var item = await PayOneAsync(id, utcNow);
                if (item is not null)
                {
                    paid.Add(item);
                }
            }
            return paid;
        }

        private async Task<MaturedDeposit?> PayOneAsync(int depositId, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var deposit = await _dbContext.Deposits.FirstOrDefaultAsync(d => d.Id == depositId);
                if (deposit is null || deposit.Status != DepositStatus.Active || deposit.MaturesAt > utcNow)
                {
                    return null;
                }

                // Pul seçilmiş karta qayıdır; kart bloklanıbsa və ya yoxdursa müştərinin başqa yararlı kartına. Heç biri yoxdursa növbəti işə qədər gözləyir
                var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == deposit.PayoutCardId);
                if (card is null || card.IsBlocked || card.Tier == CardTier.Cashback)
                {
                    card = await _dbContext.Cards
                        .Where(c => c.UserId == deposit.UserId && !c.IsBlocked && c.Tier != CardTier.Cashback)
                        .OrderBy(c => c.Id)
                        .FirstOrDefaultAsync();
                }
                if (card is null)
                {
                    return null;
                }

                var reference = "DM-" + deposit.Reference.Substring(3);

                card.Balance += deposit.Principal;
                _dbContext.Transactions.Add(new Transaction
                {
                    CardId = card.Id,
                    Type = TransactionType.DepositPayout,
                    IsIncome = true,
                    Amount = deposit.Principal,
                    BalanceAfter = card.Balance,
                    Description = "Deposit returned · " + deposit.TermMonths + " months",
                    Reference = reference
                });

                if (deposit.ExpectedInterest > 0m)
                {
                    card.Balance += deposit.ExpectedInterest;
                    _dbContext.Transactions.Add(new Transaction
                    {
                        CardId = card.Id,
                        Type = TransactionType.DepositInterest,
                        IsIncome = true,
                        Amount = deposit.ExpectedInterest,
                        BalanceAfter = card.Balance,
                        Description = "Deposit interest · " + deposit.AnnualRate.ToString("0.##", CultureInfo.InvariantCulture) + "% a year",
                        Reference = reference
                    });
                }

                deposit.Status = DepositStatus.Paid;
                deposit.PaidAt = utcNow;
                deposit.PaidToCardId = card.Id;

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new MaturedDeposit(deposit.UserId, deposit.Id, deposit.Principal, deposit.ExpectedInterest,
                        card.CardNumber.Substring(card.CardNumber.Length - 4), deposit.Reference);
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
                catch (DbUpdateException)
                {
                    // (Reference, Type) unikal indeksi: bu depozit artıq ödənilib
                    await transaction.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    return null;
                }
            }

            _dbContext.ChangeTracker.Clear();
            return null;
        }
    }
}
