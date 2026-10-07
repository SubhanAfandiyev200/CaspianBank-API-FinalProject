using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CardTierConfigRepository : BaseRepository<CardTierConfig>, ICardTierConfigRepository
    {
        public CardTierConfigRepository(AppDbContext context) : base(context) { }

        // Tier unikaldır: hər növün tək qaydası var
        public async Task<CardTierConfig?> GetByTierAsync(CardTier tier)
        {
            return await _dbSet
                    .AsNoTracking()
                    .Include(c => c.CardDesign)
                    .FirstOrDefaultAsync(c => c.Tier == tier);
        }

        public async Task<IReadOnlyList<CardTierConfig>> GetAllWithDesignAsync()
        {
            return await _dbSet
                    .AsNoTracking()
                    .Include(c => c.CardDesign)
                    .OrderBy(c => c.Tier)
                    .ToListAsync();
        }
    }
}
