using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CardDesignRepository : BaseRepository<CardDesign>, ICardDesignRepository
    {
        public CardDesignRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<CardDesign>> GetHomeAsync(int max)
        {
            return await _dbSet
                    .AsNoTracking()
                    .Where(d => d.ShowOnHome)
                    .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id)
                    .Take(max)
                    .ToListAsync();
        }

        public async Task<IEnumerable<CardDesign>> GetAllOrderedAsync()
        {
            return await _dbSet
                    .AsNoTracking()
                    .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id)
                    .ToListAsync();
        }

        public async Task<int> CountShownAsync(int? exceptId)
        {
            return await _dbSet.CountAsync(d => d.ShowOnHome && (exceptId == null || d.Id != exceptId));
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            if (await _dbContext.Cards.AnyAsync(c => c.CardDesignId == id))
            {
                return true;
            }
            return await _dbContext.CardTierConfigs.AnyAsync(c => c.CardDesignId == id);
        }
    }
}
