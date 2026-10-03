using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IEmailOtpRepository : IBaseRepository<EmailOtp>
    {
        Task AddAsync(EmailOtp otp);
        Task<EmailOtp?> GetLatestActiveAsync(string email);
        Task<EmailOtp?> GetLatestAsync(string email);
        Task<EmailOtp?> GetVerifiedAsync(string email, string verificationTokenHash, DateTime verifiedAfter);
        Task InvalidateActiveAsync(string email);
        Task SaveChangesAsync();
    }
}
