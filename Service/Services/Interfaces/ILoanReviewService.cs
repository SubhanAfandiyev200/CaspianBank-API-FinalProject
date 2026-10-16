using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // İşçinin kredit baxışı: siyahı, müraciətçinin tam məlumatı, təsdiq və rədd
    public interface ILoanReviewService
    {
        Task<ServiceResult<LoanReviewPageDto>> GetPageAsync(LoanReviewFilterDto filter);
        Task<ServiceResult<LoanReviewDetailDto>> GetDetailAsync(int loanId);

        // Təsdiq olunan məbləğ istənilənə bərabərdirsə kredit dərhal verilir, azdırsa müştəriyə təklif (Offered) gedir
        Task<ServiceResult<LoanSummaryDto>> ApproveAsync(string staffUserId, int loanId, ApproveLoanDto model);
        Task<ServiceResult<LoanSummaryDto>> RejectAsync(string staffUserId, int loanId, RejectLoanDto model);
    }
}
