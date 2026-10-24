using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    // Müştərinin xərc analitikası üçün xam məlumat
    public interface IAnalyticsRepository
    {
        // Müştərinin kartlarından çıxan pul (öz kartları arasında köçürmə daxil deyil): [fromUtc, indi)
        Task<List<OutgoingRow>> GetOutgoingAsync(string userId, DateTime fromUtc);
    }

    public record OutgoingRow(TransactionType Type, decimal Amount, DateTime CreatedAt);
}
