using Service.Helpers.DTOs.Security;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Şübhəli fəaliyyət: pul əməliyyatlarından sonra sistem qaydalara baxır (Monitor), Security işçisi siqnallara baxır (Admin)
    public interface ISecurityMonitor
    {
        // Müştəri başqasına (başqa müştəriyə və ya başqa banka) pul göndərdikdən sonra çağırılır. Xəta əsas əməliyyatı pozmur
        Task EvaluateTransferAsync(string userId, decimal amount, string reference);

        // Təkrarlanan uğursuz girişdən sonra hesab müvəqqəti bloklananda çağırılır
        Task OnAccountLockedAsync(string userId);
    }

    public interface ISecurityAlertService
    {
        Task<ServiceResult<SecurityAlertPageDto>> GetPageAsync(SecurityAlertFilterDto filter);

        // Siqnala baxılır: "təhlükəsizdir" və ya "şübhəlidir, hesabı dondur" (dondurma müştəri hesabını işçinin adı ilə dondurur)
        Task<ServiceResult<SecurityAlertDto>> ResolveAsync(string staffName, int id, ResolveAlertDto model);
    }
}
