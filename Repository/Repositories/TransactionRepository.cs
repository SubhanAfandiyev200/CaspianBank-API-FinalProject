using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Transaction>> GetByCardAsync(int cardId, int take)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.CardId == cardId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Take(take)
                .ToListAsync();
        }
    }
}
