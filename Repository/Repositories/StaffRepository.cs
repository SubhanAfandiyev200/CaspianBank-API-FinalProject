using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;

        public StaffRepository(UserManager<AppUser> userManager, AppDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<List<(AppUser User, string Role)>> GetAllAsync()
        {
            var rows = await (from u in _dbContext.Users.AsNoTracking()
                              join ur in _dbContext.UserRoles on u.Id equals ur.UserId
                              join r in _dbContext.Roles on ur.RoleId equals r.Id
                              where r.Name != Roles.Customer
                              select new { User = u, Role = r.Name! })
                             .ToListAsync();

            return rows
                .OrderBy(r => r.Role == Roles.SuperAdmin ? 0 : 1)
                .ThenBy(r => r.User.Name)
                .ThenBy(r => r.User.Surname)
                .Select(r => (r.User, r.Role))
                .ToList();
        }

        public Task<AppUser?> FindAsync(string id)
        {
            return _userManager.FindByIdAsync(id);
        }

        public async Task<string?> GetRoleAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault(r => r != Roles.Customer);
        }

        public async Task<IdentityResult> CreateAsync(AppUser user, string temporaryPassword, string role)
        {
            var created = await _userManager.CreateAsync(user, temporaryPassword);
            if (!created.Succeeded)
            {
                return created;
            }

            var added = await _userManager.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                // Rolsuz hesab qalmasın
                await _userManager.DeleteAsync(user);
            }
            return added;
        }

        public async Task<IdentityResult> ChangeRoleAsync(AppUser user, string newRole)
        {
            var current = await _userManager.GetRolesAsync(user);
            var removed = await _userManager.RemoveFromRolesAsync(user, current);
            if (!removed.Succeeded)
            {
                return removed;
            }

            var added = await _userManager.AddToRoleAsync(user, newRole);
            if (!added.Succeeded)
            {
                // Köhnə rolu qaytar ki, işçi rolsuz qalmasın
                await _userManager.AddToRolesAsync(user, current);
            }
            return added;
        }

        public Task<IdentityResult> UpdateAsync(AppUser user)
        {
            return _userManager.UpdateAsync(user);
        }
    }
}
