using Domain.Entities;
using Domain.Enums;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    // Kreditlər. Oxuma metodları izləmir (AsNoTracking); pul və vəziyyət dəyişən metodlar özü tranzaksiya açıb atomik icra edir
    public interface ILoanRepository
    {
        // ---------- Oxuma ----------

        // Müştərinin kreditləri (yeni əvvəldə), aylıq ödənişləri ilə
        Task<IReadOnlyList<Loan>> GetByBorrowerAsync(string userId);

        // Müştərinin bir krediti: aylıq ödənişlər, ödəniş tarixçəsi və pulun düşdüyü kart ilə. Başqasının krediti üçün null
        Task<Loan?> GetDetailForBorrowerAsync(int loanId, string userId);

        // İşçi baxışı: sahibi, aylıq ödənişlər və ödəniş tarixçəsi ilə
        Task<Loan?> GetDetailAsync(int loanId);

        // Ödəniş kodu ilə (Bills): sahibi və aylıq ödənişlər ilə
        Task<Loan?> GetByContractNumberAsync(string contractNumber);

        // Müştərinin açıq kreditlərinin sayı (Pending, Offered, Active, Court): eyni anda ən çox MaxActiveLoans
        Task<int> CountOpenAsync(string userId);

        // Müştərinin Active və Court kreditlərinin aylıq ödənişlərinin cəmi (maaşa nisbət limiti üçün)
        Task<decimal> SumActiveMonthlyPaymentsAsync(string userId);

        // Müştərinin bütün Caspian kreditləri, aylıq ödənişləri ilə (işçi baxışında "Caspian-dakı tarixçə")
        Task<IReadOnlyList<Loan>> GetAllByBorrowerWithInstallmentsAsync(string userId);

        Task<LoanReviewPage> GetReviewPageAsync(LoanStatus? status, string? search, int skip, int take);

        // ---------- Yazma ----------

        // Yeni müraciət: ödəniş kodu (ContractNumber) burada verilir
        Task<Loan> CreateAsync(Loan loan, DateTime utcNow);

        // İşçinin qərarı: approvedAmount istənilən məbləğə bərabərdirsə kredit dərhal aktivləşir və pul karta düşür,
        // az olarsa müştərinin razılığı üçün təklif (Offered) yaranır
        Task<LoanResult> ApproveAsync(int loanId, string staffUserId, decimal approvedAmount, DateTime utcNow);

        Task<LoanResult> RejectAsync(int loanId, string staffUserId, string reason, DateTime utcNow);

        // Müştəri təklifə razılaşır: kredit aktivləşir, pul karta düşür (targetCardId verilsə pul həmin öz kartına düşür)
        Task<LoanResult> AcceptOfferAsync(int loanId, string userId, int? targetCardId, DateTime utcNow);

        Task<LoanResult> DeclineOfferAsync(int loanId, string userId, DateTime utcNow);

        // Ödəniş (aylıq, hissəvi erkən, tam erkən): kartdan çıxır, cədvəl və tarixçə yenilənir
        Task<LoanResult> PayAsync(LoanPaymentPlan plan);

        // Eyni Reference ikinci dəfə gələndə (forma iki dəfə göndərilib) əvvəlki ödənişin qəbzi. Yalnız ödəyənin özünün ödənişi qaytarılır
        Task<LoanResult?> FindPaymentAsync(string reference, string userId);

        // Gündəlik iş: vaxtı bitmiş təkliflər Declined olur, gecikmiş kreditlərə cərimə yazılır, 90 gün gecikənlər Court olur.
        // Dəyişən kredit sayını və müştəriyə deyilməli hadisələri (təklif bitdi, ödəniş yaxınlaşır, gecikmə, Court) qaytarır
        Task<LoanMaintenanceResult> RunMaintenanceAsync(DateTime utcNow);
    }
}
