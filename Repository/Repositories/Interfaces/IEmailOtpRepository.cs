using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IEmailOtpRepository : IBaseRepository<EmailOtp>
    {
        // Bütün metodlar yalnız verilən məqsədin (Purpose) kodlarına baxır
        Task<EmailOtp?> GetLatestActiveAsync(string email, string purpose);
        Task<EmailOtp?> GetLatestAsync(string email, string purpose);
        Task<EmailOtp?> GetVerifiedAsync(string email, string verificationTokenHash, DateTime verifiedAfter);
        Task InvalidateActiveAsync(string email, string purpose);
    }
}
