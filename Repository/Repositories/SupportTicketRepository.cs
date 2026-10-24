using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class SupportTicketRepository : BaseRepository<SupportTicket>, ISupportTicketRepository
    {
        public SupportTicketRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<SupportTicket>> GetByUserAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Messages)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.LastActivityAt)
                .ThenByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<int> CountOpenByUserAsync(string userId)
        {
            return await _dbSet.AsNoTracking().CountAsync(t => t.UserId == userId && t.Status != TicketStatus.Closed);
        }

        public async Task<SupportTicket?> FindOpenHelpTicketAsync(string userId, DateTime since)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.Messages)
                .Where(t => t.UserId == userId && t.Status != TicketStatus.Closed && t.Subject.StartsWith("Help: ") && t.LastActivityAt >= since)
                .OrderByDescending(t => t.LastActivityAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountOpenByGuestAsync(string email)
        {
            return await _dbSet.AsNoTracking().CountAsync(t => t.GuestEmail == email && t.Status != TicketStatus.Closed);
        }

        public async Task<SupportTicket?> GetWithMessagesAsync(int id, string? userId)
        {
            var query = _dbSet.Include(t => t.User).Include(t => t.Messages).AsQueryable();
            if (userId is not null)
            {
                query = query.Where(t => t.UserId == userId);
            }

            // Mesajların sırası xidmətdə (köhnədən yeniyə) qoyulur: izlənən kolleksiyanı burada əvəz etmirik
            return await query.FirstOrDefaultAsync(t => t.Id == id);

        }

        public async Task<(IReadOnlyList<SupportTicket> Items, int Total, int PendingCount)> GetPageAsync(TicketStatus? status, string? search, int skip, int take)
        {
            var pending = await _dbSet.AsNoTracking().CountAsync(t => t.Status == TicketStatus.Pending);

            var query = _dbSet.AsNoTracking().Include(t => t.User).Include(t => t.Messages).AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = "%" + search.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
                query = query.Where(t => EF.Functions.Like(t.Subject, pattern)
                                      || EF.Functions.Like(t.User.Name + " " + t.User.Surname, pattern)
                                      || EF.Functions.Like(t.User.Email!, pattern)
                                      || EF.Functions.Like(t.GuestEmail!, pattern));
            }

            var total = await query.CountAsync();
            // Gözləyənlər əvvəldə (işçinin görəcəyi iş), sonra ən yeni fəaliyyət
            var items = await query
                .OrderBy(t => t.Status == TicketStatus.Pending ? 0 : (t.Status == TicketStatus.Answered ? 1 : 2))
                .ThenByDescending(t => t.LastActivityAt)
                .ThenByDescending(t => t.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return (items, total, pending);
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
