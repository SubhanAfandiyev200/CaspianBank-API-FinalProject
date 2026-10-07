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
    }
}
