using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    // Aylıq təkrarlanan ödənişlər (Bills). Pul hərəkəti burada yoxdur: ödənişi servis mövcud ödəniş əməliyyatı ilə edir
    public interface IRecurringPaymentRepository : IBaseRepository<RecurringPayment>
    {
        // Müştərinin siyahısı (təchizatçısı və kartı ilə, izləmir), köhnədən yeniyə
        Task<IReadOnlyList<RecurringPayment>> GetByUserAsync(string userId);

        // Eyni təchizatçı və nömrə üçün artıq varsa onu tapır (izləyir: yeniləmək üçün). Kredit üçün providerId = null
        Task<RecurringPayment?> FindAsync(string userId, int? providerId, string identifier);

        // Müştərinin öz qeydi (izləyir, təchizatçı və kartla). Başqasınındırsa null
        Task<RecurringPayment?> GetOwnedAsync(int id, string userId);

        Task<int> CountByUserAsync(string userId);

        // Gündəlik iş: aktiv olub bu ay üçün hələ işlənməyənlərin nömrələri
        Task<IReadOnlyList<int>> GetDueIdsAsync(string period);

        // Gündəlik iş üçün bir qeyd (izləyir, təchizatçı və kartla)
        Task<RecurringPayment?> GetForRunAsync(int id);

        // Yalnız bu qeydi yazır. IBaseRepository.UpdateAsync əvəzinə bunu işlət: o, Include ilə gələn kart və təchizatçını da
        // "dəyişib" sayır, kartın köhnəlmiş RowVersion-u ilə yazmağa çalışıb concurrency xətası verir (ödəniş artıq keçdikdən sonra)
        Task SaveAsync(RecurringPayment item);
    }
}
