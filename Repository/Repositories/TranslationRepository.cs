using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class TranslationRepository : ITranslationRepository
    {
        private readonly AppDbContext _dbContext;

        public TranslationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Dictionary<string, string>> GetDictionaryAsync(string language)
        {
            return await _dbContext.Translations.AsNoTracking()
                .Where(t => t.Language == language)
                .ToDictionaryAsync(t => t.Key, t => t.Value);
        }

        public async Task<(List<string> Keys, int Total)> GetKeysPageAsync(string? search, bool onlyMissing, string[] languages, int page, int pageSize)
        {
            var query = _dbContext.Translations.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(t => t.Key.Contains(term) || t.Value.Contains(term));
            }

            var keys = query.Select(t => t.Key).Distinct();
            if (onlyMissing)
            {
                // Hər dil üçün tərcüməsi olmayan açarlar
                var all = _dbContext.Translations.AsNoTracking();
                keys = keys.Where(k => all.Where(t => t.Key == k).Select(t => t.Language).Distinct().Count() < languages.Length);
            }

            var total = await keys.CountAsync();
            var items = await keys.OrderBy(k => k).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<List<Translation>> GetForKeysAsync(IEnumerable<string> keys)
        {
            var list = keys.ToList();
            return await _dbContext.Translations.AsNoTracking().Where(t => list.Contains(t.Key)).ToListAsync();
        }

        public async Task UpsertAsync(string language, string key, string value)
        {
            _dbContext.ChangeTracker.Clear();
            var row = await _dbContext.Translations.FirstOrDefaultAsync(t => t.Language == language && t.Key == key);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (row is not null)
                {
                    _dbContext.Translations.Remove(row);
                    await _dbContext.SaveChangesAsync();
                }
                _dbContext.ChangeTracker.Clear();
                return;
            }

            if (row is null)
            {
                _dbContext.Translations.Add(new Translation { Language = language, Key = key, Value = value });
            }
            else
            {
                row.Value = value;
            }
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }
    }
}
