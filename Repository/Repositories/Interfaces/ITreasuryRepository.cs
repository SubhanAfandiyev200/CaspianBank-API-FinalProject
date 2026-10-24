using Domain.Entities;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    // Bankın xəzinəsi (öz vəsaiti). Yalnız oxunur: sətirləri AppDbContext əməliyyatlarla birlikdə yazır
    public interface ITreasuryRepository
    {
        Task<decimal> GetBalanceAsync();
        Task<decimal> GetCustomerMoneyAsync();
        Task<(decimal In, decimal Out)> GetFlowAsync(DateTime fromUtc, DateTime toUtc);
        Task<bool> ReferenceExistsAsync(string reference);
        Task AddEntriesAsync(IEnumerable<TreasuryEntry> entries);
        Task<(List<TreasuryRow> Items, int Total)> GetPagedAsync(TreasuryEntryKind? kind, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize);
    }
}
