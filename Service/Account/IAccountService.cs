using Service.Account.DTOs;

namespace Service.Account
{
    public interface IAccountService
    {
        Task<RegisterResponse> RegisterAsync(RegisterDto model);
    }
}
