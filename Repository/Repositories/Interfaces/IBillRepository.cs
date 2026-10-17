using Domain.Entities;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    // Ödənişlər. Oxuma metodları izləmir (AsNoTracking); pul dəyişən metodlar özü tranzaksiya açıb atomik icra edir
    public interface IBillRepository
    {
        // ---------- Təchizatçılar ----------

        // Sıra ilə (SortOrder). onlyActive: müştəriyə yalnız aktivlər göstərilir, admin hamısını görür
        Task<IReadOnlyList<BillProvider>> GetProvidersAsync(bool onlyActive);
        Task<BillProvider?> GetProviderAsync(int id);

        // ---------- Abunəçilər ----------

        // Təchizatçıda nömrə və ya kod ilə axtarış (təchizatçısı ilə). Yoxdursa null
        Task<BillSubscriber?> FindSubscriberAsync(int providerId, string identifier);

        // Abunəçinin ən son aylıq hesabı (axtarışda "bu ay nə qədər yazılıb" göstərmək üçün)
        Task<BillCharge?> GetLastChargeAsync(int subscriberId);

        // ---------- Ödəniş ----------

        // Pul ödəyənin kartından çıxır, abunəçinin balansı artır (əvvəl borc bağlanır), cashback varsa Cashback kartına düşür. Hamısı bir tranzaksiyada
        Task<BillResult> PayAsync(BillPaymentPlan plan);

        // Eyni Reference ilə əvvəlki ödənişin qəbzi (forma iki dəfə göndərilibsə)
        Task<BillResult?> FindPaymentAsync(string reference, string userId);

        Task<IReadOnlyList<BillPayment>> GetRecentPaymentsAsync(string userId, int take);

        // Bu aydakı ödənişlərin cəmi, sayı və qazanılan cashback (səhifənin üst xülasəsi)
        Task<(decimal Paid, int Count, decimal Cashback)> GetSummaryAsync(string userId, DateTime fromUtc);

        // ---------- Gündəlik iş ----------

        // Bu ay üçün hesabı hələ yazılmamış internet, işıq, qaz və su abunəçilərinə hesab yazır. Yazılan hesabların sayını qaytarır
        Task<int> RunMonthlyBillingAsync(DateTime utcNow);
    }
}
