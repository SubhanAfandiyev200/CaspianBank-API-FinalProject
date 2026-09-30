using Service.Helpers.Responses;
using Service.Helpers.Responses.DTOs.Account;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
    }
}
