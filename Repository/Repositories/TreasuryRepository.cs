using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class TreasuryRepository : ITreasuryRepository
    {
        private readonly AppDbContext _dbContext;

        public TreasuryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> GetBalanceAsync()
        {
            return await _dbContext.TreasuryEntries.AsNoTracking().SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        // Müştərilərə borc olan pul: kartlardakı balanslar və aktiv depozitlər (məcburi ehtiyat bunun faizidir)
        public async Task<decimal> GetCustomerMoneyAsync()
        {
            var onCards = await _dbContext.Cards.AsNoTracking().SumAsync(c => (decimal?)c.Balance) ?? 0m;
            var inDeposits = await _dbContext.Deposits.AsNoTracking()
                .Where(d => d.Status == DepositStatus.Active)
                .SumAsync(d => (decimal?)d.Principal) ?? 0m;
            return onCards + inDeposits;
        }

        public async Task<bool> ReferenceExistsAsync(string reference)
        {
            return await _dbContext.TreasuryEntries.AsNoTracking().AnyAsync(t => t.Reference == reference);
        }

        public async Task AddEntriesAsync(IEnumerable<TreasuryEntry> entries)
        {
            _dbContext.TreasuryEntries.AddRange(entries);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task<(decimal In, decimal Out)> GetFlowAsync(DateTime fromUtc, DateTime toUtc)
        {
            // Başlanğıc kapital gəlir sayılmır
            var rows = _dbContext.TreasuryEntries.AsNoTracking().Where(t => t.Kind != TreasuryEntryKind.Capital && t.CreatedAt >= fromUtc && t.CreatedAt < toUtc);
            var income = await rows.Where(t => t.Amount > 0m).SumAsync(t => (decimal?)t.Amount) ?? 0m;
            var expense = await rows.Where(t => t.Amount < 0m).SumAsync(t => (decimal?)t.Amount) ?? 0m;
            return (income, -expense);
        }

        public async Task<(List<TreasuryRow> Items, int Total)> GetPagedAsync(TreasuryEntryKind? kind, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize)
        {
            var query = _dbContext.TreasuryEntries.AsNoTracking().AsQueryable();
            if (kind.HasValue)
            {
                query = query.Where(t => t.Kind == kind.Value);
            }
            if (fromUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt < toUtc.Value);
            }

            var total = await query.CountAsync();
            var entries = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Hər sətirdən sonrakı balans: həmin sətrə qədər (daxil) bütün sətirlərin cəmi. Süzgəc balansı dəyişmir
            var rows = new List<TreasuryRow>();
            foreach (var entry in entries)
            {
                var balanceAfter = await _dbContext.TreasuryEntries.AsNoTracking()
                    .Where(t => t.CreatedAt < entry.CreatedAt || (t.CreatedAt == entry.CreatedAt && t.Id <= entry.Id))
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;
                rows.Add(new TreasuryRow(entry, balanceAfter));
            }
            return (rows, total);
        }
    }
}
