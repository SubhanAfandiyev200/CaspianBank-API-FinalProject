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
    }
}
