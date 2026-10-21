using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IReportRepository
    {
        // Müştərinin öz kartının əməliyyatı (başqasının əməliyyatı tapılmır)
        Task<Transaction?> FindTransactionForUserAsync(int transactionId, string userId);

        // Qarşı tərəf: köçürmənin alan kartının sahibi (başqa müştəriyə köçürmə deyilsə null)
        Task<string?> FindRecipientUserIdAsync(string reference);

        // Yazılır. Eyni əməliyyat artıq bildirilibsə false
        Task<bool> TryAddAsync(TransactionReport report);

        Task<List<TransactionReport>> GetByUserAsync(string userId);
        Task<(List<TransactionReport> Items, int Total)> GetPagedAsync(ReportStatus? status, string? search, int page, int pageSize);
        Task<TransactionReport?> GetAsync(int id);
        Task SaveAsync();
        Task<(int Pending, int ResolvedWeek)> GetCountsAsync(DateTime weekAgoUtc);

        // Kartı bloklayır (Cashback kartı bloklanmır). Kartın etiketini qaytarır, bloklanmayıbsa null
        Task<string?> BlockCardAsync(int cardId);
    }
}
