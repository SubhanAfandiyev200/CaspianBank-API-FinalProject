using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
        Task<LoginResponse> LoginAsync(LoginDto model);
        Task<CheckEmailResponse> CheckEmailAsync(CheckEmailDto model);

        // Qeydiyyatdan əvvəl "Bu sizsiniz?" təsdiqi: FİN + vəsiqə nömrəsi reyestrdə eyni vətəndaşa aiddirmi
        Task<CheckIdentityResponse> CheckIdentityAsync(CheckIdentityDto model);

        // Giriş etmiş müştərinin profili (şəkil reyestrdən). Hesab tapılmasa null
        Task<ProfileDto?> GetProfileAsync(string userId);
    }
}
