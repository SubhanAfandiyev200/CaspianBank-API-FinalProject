using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Repository.Repositories.Interfaces
{
    // İşçi hesabları: Customer rolundan başqa rolu olan istifadəçilər. Hər işçinin tək rolu olur
    public interface IStaffRepository
    {
        // Bütün işçilər və rolları (SuperAdmin əvvəldə, sonra ada görə)
        Task<List<(AppUser User, string Role)>> GetAllAsync();

        Task<AppUser?> FindAsync(string id);
        Task<string?> GetRoleAsync(AppUser user);

        // Yeni işçi hesabı: təsadüfi şifrə ilə yaradılır, rol verilir (şifrəni işçi özü link ilə seçir)
        Task<IdentityResult> CreateAsync(AppUser user, string temporaryPassword, string role);

        // Köhnə rol(lar) silinir, yenisi verilir
        Task<IdentityResult> ChangeRoleAsync(AppUser user, string newRole);

        Task<IdentityResult> UpdateAsync(AppUser user);
    }
}
