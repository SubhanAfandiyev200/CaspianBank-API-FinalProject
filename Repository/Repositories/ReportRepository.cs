using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _dbContext;

        public ReportRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Transaction?> FindTransactionForUserAsync(int transactionId, string userId)
        {
            return await _dbContext.Transactions.AsNoTracking()
                .Include(t => t.Card)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.Card.UserId == userId);
        }

        public async Task<string?> FindRecipientUserIdAsync(string reference)
        {
            var row = await _dbContext.Transactions.AsNoTracking()
                .Include(t => t.Card)
                .FirstOrDefaultAsync(t => t.Reference == reference && t.Type == TransactionType.TransferIn);
            return row?.Card.UserId;
        }

        public async Task<bool> TryAddAsync(TransactionReport report)
        {
            _dbContext.ChangeTracker.Clear();
            if (await _dbContext.TransactionReports.AnyAsync(r => r.TransactionId == report.TransactionId))
            {
                return false;
            }
            _dbContext.TransactionReports.Add(report);
            try
            {
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<List<TransactionReport>> GetByUserAsync(string userId)
        {
            return await _dbContext.TransactionReports.AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<TransactionReport> Items, int Total)> GetPagedAsync(ReportStatus? status, string? search, int page, int pageSize)
        {
            var query = _dbContext.TransactionReports.AsNoTracking().Include(r => r.User).AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(r => r.TransactionReference.Contains(term) || r.Reason.Contains(term) || r.Description.Contains(term)
                                        || (r.User.Email != null && r.User.Email.Contains(term))
                                        || r.User.Name.Contains(term) || r.User.Surname.Contains(term));
            }

            var total = await query.CountAsync();
            // Baxılmamışlar əvvəl, sonra ən yeni
            var items = await query
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<TransactionReport?> GetAsync(int id)
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.TransactionReports.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        public async Task<(int Pending, int ResolvedWeek)> GetCountsAsync(DateTime weekAgoUtc)
        {
            var reports = _dbContext.TransactionReports.AsNoTracking();
            var pending = await reports.CountAsync(r => r.Status == ReportStatus.Pending);
            var resolved = await reports.CountAsync(r => r.Status != ReportStatus.Pending && r.ResolvedAt >= weekAgoUtc);
            return (pending, resolved);
        }

        public async Task<string?> BlockCardAsync(int cardId)
        {
            _dbContext.ChangeTracker.Clear();
            var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card is null || card.Tier == CardTier.Cashback)
            {
                return null;
            }

            card.IsBlocked = true;
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            return card.Tier + " •••• " + card.CardNumber.Substring(card.CardNumber.Length - 4);
        }
    }
}
