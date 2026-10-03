using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IPasswordService
    {
        Task<OperationResponse> ForgotPasswordAsync(ForgotPasswordDto model);
        Task<OperationResponse> ResetPasswordAsync(ResetPasswordDto model);
    }
}
