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

        // Giriş etmiş istifadəçi üçün əməliyyat kodu (məs. kartı bloklamaq): emailə 6 rəqəmli kod göndərir.
        // purpose: Domain/Constants/OtpPurposes.cs. 60 saniyədən tez yeni kod verilmir (RetryAfterSeconds)
        Task<SendOtpResponse> SendCodeAsync(string email, string purpose, string subject, string intro);

        // Kodu yoxlayır və istifadə olunmuş sayır (5 səhv cəhd kodu ləğv edir). IsSuccess: kod düzgündür
        Task<VerifyOtpResponse> ConfirmCodeAsync(string email, string purpose, string code);
    }
}
