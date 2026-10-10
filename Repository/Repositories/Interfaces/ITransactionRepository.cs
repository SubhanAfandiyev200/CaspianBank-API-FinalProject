using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ITransactionRepository : IBaseRepository<Transaction>
    {
        // Kartın ən son əməliyyatları (yeni əvvəldə)
        Task<IReadOnlyList<Transaction>> GetByCardAsync(int cardId, int take);

        // İstifadəçinin BÜTÜN kartlarının ən son əməliyyatları (yeni əvvəldə), kart məlumatı ilə
        Task<IReadOnlyList<Transaction>> GetRecentForUserAsync(string userId, int take);
    }
}
