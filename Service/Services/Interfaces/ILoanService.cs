using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Müştərinin kredit əməliyyatları. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    public interface ILoanService
    {
        // Müraciət formasının qaydaları və müştərinin müraciət edə bilib bilməməsi
        Task<ServiceResult<LoanOptionsDto>> GetOptionsAsync(string userId);

        Task<IEnumerable<LoanSummaryDto>> GetMyLoansAsync(string userId);
        Task<ServiceResult<LoanDetailDto>> GetMyLoanAsync(string userId, int loanId);

        // Yeni müraciət (Pending). Qara siyahıdakı vətəndaşın müraciəti dərhal rədd olunur
        Task<ServiceResult<LoanSummaryDto>> ApplyAsync(string userId, ApplyLoanDto model);

        // İşçi azaldılmış məbləğ təklif edibsə (Offered) müştəri razılaşır və ya imtina edir
        Task<ServiceResult<LoanSummaryDto>> AcceptOfferAsync(string userId, int loanId, AcceptLoanOfferDto model);
        Task<ServiceResult<LoanSummaryDto>> DeclineOfferAsync(string userId, int loanId);

        // Ödəniş: aylıq (gecikmiş + növbəti), hissəvi erkən və ya tam bağlama. Eyni RequestId təkrar gələndə əvvəlki qəbz qaytarılır
        Task<ServiceResult<LoanReceiptDto>> PayAsync(string userId, int loanId, PayLoanDto model);

        // Ödəniş kodu ilə (Bills): sahibin adı gizlədilmiş görünür; aylıq ödənişi kodu bilən hər kəs ödəyə bilər
        Task<ServiceResult<LoanLookupDto>> LookupAsync(string contractNumber);
        Task<ServiceResult<LoanReceiptDto>> PayByCodeAsync(string userId, PayByCodeDto model);

        // Gündəlik iş (arxa plan xidməti çağırır): vaxtı bitmiş təkliflər, cərimə, Court. Dəyişən kredit sayını qaytarır
        Task<int> RunDailyMaintenanceAsync();
    }
}
