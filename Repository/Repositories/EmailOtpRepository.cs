using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class EmailOtpRepository : BaseRepository<EmailOtp>, IEmailOtpRepository
    {
        public EmailOtpRepository(AppDbContext context) : base(context)
        {
        }

        // Ən son yaradılmış, hələ istifadə olunmamış və vaxtı keçməmiş kod (yalnız bu məqsəd üçün)
        public Task<EmailOtp?> GetLatestActiveAsync(string email, string purpose)
        {
            return _dbSet
                    .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();
        }

        // Ən son kod (istifadə olunub-olunmamasından asılı olmayaraq): yenidən göndərmə gözləmə müddəti üçün
        public Task<EmailOtp?> GetLatestAsync(string email, string purpose)
        {
            return _dbSet
                    .Where(o => o.Email == email && o.Purpose == purpose)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();
        }

        public Task<EmailOtp?> GetVerifiedAsync(string email, string verificationTokenHash, DateTime verifiedAfter)
        {
            return _dbSet.FirstOrDefaultAsync(o =>
                    o.Email == email
                    && o.VerificationTokenHash == verificationTokenHash
                    && o.VerifiedAt != null
                    && o.VerifiedAt >= verifiedAfter);
        }

        // Yeni kod göndəriləndə eyni məqsədli köhnə aktiv kodlar etibarsız olur
        public Task InvalidateActiveAsync(string email, string purpose)
        {
            return _dbSet
                    .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsUsed, true));
        }
    }
}
