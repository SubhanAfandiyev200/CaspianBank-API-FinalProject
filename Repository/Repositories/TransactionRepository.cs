using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Models;
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

        public async Task<IReadOnlyList<Transaction>> GetRecentForUserAsync(string userId, int take)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Card)
                .Where(t => t.Card.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<TransactionPageResult> GetPageForUserAsync(string userId, int? cardId, TransactionType? type, bool? isIncome,
                                                                     DateTime? fromUtc, DateTime? toUtc, string? search, int skip, int take)
        {
            // Əsas sorğu: yalnız bu istifadəçinin kartlarının əməliyyatları (başqasınınkı heç vaxt gəlmir)
            var query = _dbSet
                .AsNoTracking()
                .Where(t => t.Card.UserId == userId);

            // Hər filtr yalnız verilibsə tətbiq olunur
            if (cardId.HasValue)
            {
                query = query.Where(t => t.CardId == cardId.Value);
            }
            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }
            if (isIncome.HasValue)
            {
                query = query.Where(t => t.IsIncome == isIncome.Value);
            }
            if (fromUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt < toUtc.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var text = search.Trim();
                query = query.Where(t => t.Reference.Contains(text)
                                      || t.Description.Contains(text)
                                      || (t.Note != null && t.Note.Contains(text)));
            }

            // Say və cəmlər BÜTÜN filtrə görədir (yalnız cari səhifəyə yox)
            var totalCount = await query.CountAsync();
            var totalIn = await query.Where(t => t.IsIncome).SumAsync(t => t.Amount);
            var totalOut = await query.Where(t => !t.IsIncome).SumAsync(t => t.Amount);

            // Səhifənin özü: kart məlumatı ilə (hansı kartın əməliyyatı olduğunu göstərmək üçün)
            var items = await query
                .Include(t => t.Card)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new TransactionPageResult
            {
                Items = items,
                TotalCount = totalCount,
                TotalIn = totalIn,
                TotalOut = totalOut
            };
        }

        public async Task<Transaction?> GetLastBeforeAsync(int cardId, DateTime beforeUtc)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.CardId == cardId && t.CreatedAt < beforeUtc)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<Transaction>> GetByCardInRangeAsync(int cardId, DateTime fromUtc, DateTime toUtc)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.CardId == cardId && t.CreatedAt >= fromUtc && t.CreatedAt < toUtc)
                .OrderBy(t => t.CreatedAt)
                .ThenBy(t => t.Id)
                .ToListAsync();
        }
    }
}
