using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<AppUser> _userManager;

        public AccountRepository(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public Task<IdentityResult> RegisterAsync(AppUser user, string password)
            => _userManager.CreateAsync(user, password);

        public Task<IdentityResult> AddToRoleAsync(AppUser user, string role)
            => _userManager.AddToRoleAsync(user, role);

        public Task<AppUser?> GetByEmailAsync(string email)
            => _userManager.FindByEmailAsync(email);

        public Task<bool> CheckPasswordAsync(AppUser user, string password)
            => _userManager.CheckPasswordAsync(user, password);

        public Task<IList<string>> GetRolesAsync(AppUser user)
            => _userManager.GetRolesAsync(user);

        public Task<string> GeneratePasswordResetTokenAsync(AppUser user)
            => _userManager.GeneratePasswordResetTokenAsync(user);

        public Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword)
            => _userManager.ResetPasswordAsync(user, token, newPassword);

        public Task<bool> IsLockedOutAsync(AppUser user)
            => _userManager.IsLockedOutAsync(user);

        // Limit (Program.cs-də 5) aşılanda Identity hesabı özü müvəqqəti bloklayır
        public async Task RegisterFailedAttemptAsync(AppUser user)
            => await _userManager.AccessFailedAsync(user);

        public async Task ResetFailedAttemptsAsync(AppUser user)
            => await _userManager.ResetAccessFailedCountAsync(user);
    }
}
