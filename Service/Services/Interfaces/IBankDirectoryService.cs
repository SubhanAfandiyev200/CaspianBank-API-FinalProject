using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IBankDirectoryService
    {
        // 8 rəqəmli BIN-dən bank: başqa bank, Caspian və ya tapılmadı (NotFound)
        Task<ServiceResult<BankLookupDto>> LookupAsync(string? bin);
    }
}
