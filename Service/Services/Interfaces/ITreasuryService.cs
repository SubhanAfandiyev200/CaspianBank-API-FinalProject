using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Bankın xəzinəsi və jurnalı (yalnız oxuma)
    public interface ITreasuryService
    {
        Task<ServiceResult<TreasuryPageDto>> GetPageAsync(TreasuryFilterDto filter);
    }
}
