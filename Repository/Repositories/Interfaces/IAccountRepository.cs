using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Repository.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<IdentityResult> RegisterAsync(AppUser user, string password);
        Task<IdentityResult> AddToRoleAsync(AppUser user, string role);
    }
}
