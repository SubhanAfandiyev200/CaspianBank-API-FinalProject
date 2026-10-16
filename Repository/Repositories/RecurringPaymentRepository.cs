using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class RecurringPaymentRepository : BaseRepository<RecurringPayment>, IRecurringPaymentRepository
    {
        public RecurringPaymentRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<RecurringPayment>> GetByUserAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.Provider)
                .Include(r => r.Card)
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.CreatedAt)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public async Task<RecurringPayment?> FindAsync(string userId, int? providerId, string identifier)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.UserId == userId && r.ProviderId == providerId && r.Identifier == identifier);
        }

        public async Task<RecurringPayment?> GetOwnedAsync(int id, string userId)
        {
            return await _dbSet
                .Include(r => r.Provider)
                .Include(r => r.Card)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task<int> CountByUserAsync(string userId)
        {
            return await _dbSet.AsNoTracking().CountAsync(r => r.UserId == userId);
        }

        public async Task<IReadOnlyList<int>> GetDueIdsAsync(string period)
        {
            // "2026-10" yazılışı əlifba sırası ilə də vaxt sırasıdır, ona görə != kifayətdir (keçmiş ay = işlənməyib)
            return await _dbSet
                .AsNoTracking()
                .Where(r => r.IsActive && r.LastPeriod != period)
                .OrderBy(r => r.Id)
                .Select(r => r.Id)
                .ToListAsync();
        }

        public async Task<RecurringPayment?> GetForRunAsync(int id)
        {
            return await _dbSet
                .Include(r => r.Provider)
                .Include(r => r.Card)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task SaveAsync(RecurringPayment item)
        {
            // Entry(...).State yalnız bu qeydi "dəyişib" işarələyir (DbSet.Update bütün qrafı edir). Ödəniş əməliyyatı izləyicini təmizləyir,
            // ona görə qeyd ayrılmış ola bilər: bu yazılış onu yenidən qoşur
            _dbContext.Entry(item).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
