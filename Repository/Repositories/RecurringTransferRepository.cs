using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class RecurringTransferRepository : IRecurringTransferRepository
    {
        private readonly AppDbContext _dbContext;

        public RecurringTransferRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RecurringTransfer>> GetByUserAsync(string userId)
        {
            return await _dbContext.RecurringTransfers.AsNoTracking()
                .Include(r => r.SourceCard)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.DayOfMonth)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public async Task<int> CountByUserAsync(string userId)
        {
            return await _dbContext.RecurringTransfers.AsNoTracking().CountAsync(r => r.UserId == userId);
        }

        public async Task<RecurringTransfer?> GetForUserAsync(int id, string userId)
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.RecurringTransfers.Include(r => r.SourceCard).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        public async Task AddAsync(RecurringTransfer item)
        {
            _dbContext.ChangeTracker.Clear();
            _dbContext.RecurringTransfers.Add(item);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task SaveAsync(RecurringTransfer item)
        {
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task DeleteAsync(RecurringTransfer item)
        {
            _dbContext.RecurringTransfers.Remove(item);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task<List<int>> GetDueIdsAsync(string period)
        {
            return await _dbContext.RecurringTransfers.AsNoTracking()
                .Where(r => r.IsActive && r.LastPeriod != period)
                .Select(r => r.Id)
                .ToListAsync();
        }

        public async Task<RecurringTransfer?> GetForRunAsync(int id)
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.RecurringTransfers.Include(r => r.SourceCard).FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
