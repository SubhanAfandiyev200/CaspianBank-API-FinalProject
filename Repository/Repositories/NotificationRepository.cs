using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(IReadOnlyList<Notification> Items, int Total)> GetPageAsync(string userId, bool unreadOnly, int skip, int take)
        {
            var query = _dbSet.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return (items, total);
        }

        public async Task<int> CountUnreadAsync(string userId)
        {
            return await _dbSet.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkReadAsync(int id, string userId)
        {
            var notification = await _dbSet.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification is null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<int> MarkAllReadAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        }

        public async Task<bool> ExistsAsync(string userId, string dedupeKey)
        {
            return await _dbSet.AsNoTracking().AnyAsync(n => n.UserId == userId && n.DedupeKey == dedupeKey);
        }

        public async Task<bool> TryAddAsync(Notification notification)
        {
            try
            {
                await _dbSet.AddAsync(notification);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _dbContext.Entry(notification).State = EntityState.Detached;
                return false;
            }
        }
    }
}
