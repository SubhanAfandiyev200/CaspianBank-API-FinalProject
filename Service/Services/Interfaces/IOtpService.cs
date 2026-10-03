using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IOtpService
    {
        Task<SendOtpResponse> SendOtpAsync(SendOtpDto model);
        Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpDto model);

        // Register-dən əvvəl: bu email üçün etibarlı təsdiq tokeni varmı? (tokeni istifadə etmir)
        Task<bool> IsVerifiedAsync(string email, string verificationToken);

        // Register uğurlu olandan sonra: token bir dəfə işləyir
        Task ConsumeVerificationAsync(string email, string verificationToken);
    }
}
