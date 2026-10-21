using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IBankDashboardService
    {
        // month: "2026-10"; boşdursa cari ay (Bakı vaxtı). Gələcək ay və ya yanlış yazılış xətadır
        Task<ServiceResult<BankDashboardDto>> GetAsync(string? month);

        // Bir ayın əməliyyat xərci və mənfəət vergisi (xəzinə ayı bağlayanda yazılır). monthStart: Bakı ayının 1-i
        Task<(decimal OperatingCosts, decimal Tax)?> GetMonthCostsAsync(DateTime monthStart);
    }
}
