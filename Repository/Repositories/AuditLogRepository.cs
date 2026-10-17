using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPageAsync(string? search, string? action, DateTime? fromUtc, DateTime? toUtc, int skip, int take)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }
            if (fromUtc.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                query = query.Where(a => a.CreatedAt < toUtc.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = "%" + search.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
                query = query.Where(a => EF.Functions.Like(a.ActorName, pattern)
                                      || EF.Functions.Like(a.TargetLabel, pattern)
                                      || EF.Functions.Like(a.Details, pattern));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return (items, total);
        }
    }
}
