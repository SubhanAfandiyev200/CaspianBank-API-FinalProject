using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
    }
}
