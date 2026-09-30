using Service.Helpers.Responses;
using Service.Helpers.Responses.DTOs;

namespace Service.Services.Interfaces
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
    }
}
