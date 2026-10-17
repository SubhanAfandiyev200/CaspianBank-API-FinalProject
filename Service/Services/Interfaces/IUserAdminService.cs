using Service.Helpers.DTOs.Users;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // İşçilərin müştəriləri görməsi və hesabı dondurması (restrict). Staff adı HƏMİŞƏ tokendən götürülür
    public interface IUserAdminService
    {
        Task<ServiceResult<UserPageDto>> GetPageAsync(UserFilterDto filter);

        Task<ServiceResult<UserDetailDto>> GetDetailAsync(string userId);

        // Hesab dondurulur: müştəri daxil ola, köçürə, ödəniş edə və kredit götürə bilmir. Səbəb yazılır
        Task<ServiceResult<UserDetailDto>> RestrictAsync(string staffName, string userId, RestrictUserDto model);

        Task<ServiceResult<UserDetailDto>> UnrestrictAsync(string userId);
    }
}
