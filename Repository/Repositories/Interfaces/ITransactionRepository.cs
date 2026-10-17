using Domain.Entities;
using Domain.Enums;
using Repository.Models;

namespace Repository.Repositories.Interfaces
{
    // Ümumi əməliyyatlar (GetById, Add, Update, Delete) IBaseRepository-dədir; burada yalnız əməliyyat jurnalına məxsus sorğular qalır
    public interface ITransactionRepository : IBaseRepository<Transaction>
    {
        // Kartın ən son əməliyyatları (yeni əvvəldə)
        Task<IReadOnlyList<Transaction>> GetByCardAsync(int cardId, int take);

        // İstifadəçinin BÜTÜN kartlarının ən son əməliyyatları (yeni əvvəldə), kart məlumatı ilə
        Task<IReadOnlyList<Transaction>> GetRecentForUserAsync(string userId, int take);

        // Tarixçə səhifəsi: istifadəçinin bütün kartlarının əməliyyatları, verilən filtrlərlə, yeni əvvəldə.
        // Boş (null) filtr tətbiq olunmur. fromUtc daxildir, toUtc daxil deyil. Say və cəmlər səhifəyə yox, bütün filtrə görədir
        Task<TransactionPageResult> GetPageForUserAsync(string userId, int? cardId, TransactionType? type, bool? isIncome,
                                                        DateTime? fromUtc, DateTime? toUtc, string? search, int skip, int take);

        // Çıxarış üçün: kartın "beforeUtc"-dən əvvəlki ən son əməliyyatı (açılış balansı onun BalanceAfter-indən götürülür)
        Task<Transaction?> GetLastBeforeAsync(int cardId, DateTime beforeUtc);

        // Çıxarış üçün: kartın [fromUtc, toUtc) aralığındakı bütün əməliyyatları, köhnədən yeniyə
        Task<IReadOnlyList<Transaction>> GetByCardInRangeAsync(int cardId, DateTime fromUtc, DateTime toUtc);
    }
}
