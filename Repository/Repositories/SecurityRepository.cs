using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class SecurityRepository : ISecurityRepository
    {
        // Öz kartları arasında köçürmənin təsviri bu sözlə başlayır (TransferService yazır): şübhəli sayılmır
        private const string OwnTransferPrefix = "Transfer to your";

        private readonly AppDbContext _dbContext;

        public SecurityRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> TryAddAsync(SecurityAlert alert)
        {
            _dbContext.ChangeTracker.Clear();
            if (await _dbContext.SecurityAlerts.AnyAsync(a => a.Kind == alert.Kind && a.Reference == alert.Reference))
            {
                return false;
            }

            _dbContext.SecurityAlerts.Add(alert);
            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Eyni anda başqa sorğu yazıb: unikal indeks təkrarı saxlamır
                _dbContext.ChangeTracker.Clear();
                return false;
            }
        }

        private IQueryable<Transaction> Outgoing(string userId, DateTime fromUtc)
        {
            return _dbContext.Transactions.AsNoTracking()
                .Where(t => t.Card.UserId == userId
                            && t.Type == TransactionType.TransferOut
                            && t.CreatedAt >= fromUtc
                            && !t.Description.StartsWith(OwnTransferPrefix));
        }

        public async Task<int> CountOutgoingSinceAsync(string userId, DateTime fromUtc)
        {
            return await Outgoing(userId, fromUtc).CountAsync();
        }

        public async Task<decimal> SumOutgoingSinceAsync(string userId, DateTime fromUtc)
        {
            return await Outgoing(userId, fromUtc).SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<(List<SecurityAlert> Items, int Total)> GetPagedAsync(SecurityAlertStatus? status, SecurityAlertSeverity? severity,
            SecurityAlertKind? kind, string? search, int page, int pageSize)
        {
            var query = _dbContext.SecurityAlerts.AsNoTracking().Include(a => a.User).AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }
            if (severity.HasValue)
            {
                query = query.Where(a => a.Severity == severity.Value);
            }
            if (kind.HasValue)
            {
                query = query.Where(a => a.Kind == kind.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a => a.Reference.Contains(term) || a.Description.Contains(term)
                                        || (a.User.Email != null && a.User.Email.Contains(term))
                                        || a.User.Name.Contains(term) || a.User.Surname.Contains(term));
            }

            var total = await query.CountAsync();
            // Açıq siqnallar əvvəl, onlarda yüksək əvvəl, sonra ən yeni
            var items = await query
                .OrderBy(a => a.Status)
                .ThenByDescending(a => a.Status == SecurityAlertStatus.Open ? (int)a.Severity : 0)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<SecurityAlert?> GetAsync(int id)
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.SecurityAlerts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task SaveAsync(SecurityAlert alert)
        {
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task<(int Open, int OpenHigh, int ResolvedWeek)> GetCountsAsync(DateTime weekAgoUtc)
        {
            var alerts = _dbContext.SecurityAlerts.AsNoTracking();
            var open = await alerts.CountAsync(a => a.Status == SecurityAlertStatus.Open);
            var openHigh = await alerts.CountAsync(a => a.Status == SecurityAlertStatus.Open && a.Severity == SecurityAlertSeverity.High);
            var resolved = await alerts.CountAsync(a => a.Status != SecurityAlertStatus.Open && a.ResolvedAt >= weekAgoUtc);
            return (open, openHigh, resolved);
        }
    }
}
