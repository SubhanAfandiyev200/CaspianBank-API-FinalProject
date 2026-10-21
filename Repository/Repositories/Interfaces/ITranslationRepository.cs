using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ITranslationRepository
    {
        // Bir dilin bütün tərcümələri: açar → mətn
        Task<Dictionary<string, string>> GetDictionaryAsync(string language);

        // Admin siyahısı: açarlar (axtarış, yalnız tərcüməsi çatışmayanlar), hər açar üçün bütün dillərin dəyəri
        Task<(List<string> Keys, int Total)> GetKeysPageAsync(string? search, bool onlyMissing, string[] languages, int page, int pageSize);
        Task<List<Translation>> GetForKeysAsync(IEnumerable<string> keys);

        // Varsa yeniləyir, yoxdursa əlavə edir. Dəyər boşdursa tərcümə silinir (ingilis mətn göstərilir)
        Task UpsertAsync(string language, string key, string value);
    }
}
