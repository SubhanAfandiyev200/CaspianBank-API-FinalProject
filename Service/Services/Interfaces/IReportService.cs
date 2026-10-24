using Service.Helpers.DTOs.Reports;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Müştərinin şübhəli əməliyyat bildirişləri. UserId HƏMİŞƏ tokendən götürülür
    public interface IReportService
    {
        // Əməliyyat müştərinin öz kartınındır və hələ bildirilməyib. İşçilərə bildiriş gedir
        Task<ServiceResult<MyReportDto>> ReportAsync(string userId, ReportTransactionDto model);

        Task<IEnumerable<MyReportDto>> GetMineAsync(string userId);
    }

    // İşçi tərəfi: bildirişlərə baxış
    public interface IReportAdminService
    {
        Task<ServiceResult<AdminReportPageDto>> GetPageAsync(ReportFilterDto filter);

        // Review: baxıldı. Action: tədbir (qarşı tərəfin hesabı dondurulur, yoxdursa kart bloklanır). Dismiss: əsassız
        Task<ServiceResult<AdminReportDto>> ResolveAsync(string staffName, int id, ResolveReportDto model);
    }
}
