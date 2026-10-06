using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Repository.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<IdentityResult> RegisterAsync(AppUser user, string password);
        Task<IdentityResult> AddToRoleAsync(AppUser user, string role);

        Task<AppUser?> GetByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(AppUser user, string password);
        Task<IList<string>> GetRolesAsync(AppUser user);

        // Şifrə bərpası (Identity-nin öz token mexanizmi)
        Task<string> GeneratePasswordResetTokenAsync(AppUser user);
        Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword);

        // Uğursuz giriş cəhdlərinin sayı və müvəqqəti blok (lockout)
        Task<bool> IsLockedOutAsync(AppUser user);
        Task RegisterFailedAttemptAsync(AppUser user);
        Task ResetFailedAttemptsAsync(AppUser user);
    }
}
