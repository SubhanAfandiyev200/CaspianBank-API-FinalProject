using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IBankDashboardService
    {
        // month: "2026-10"; boşdursa cari ay (Bakı vaxtı). Gələcək ay və ya yanlış yazılış xətadır
        Task<ServiceResult<BankDashboardDto>> GetAsync(string? month);
    }
}
