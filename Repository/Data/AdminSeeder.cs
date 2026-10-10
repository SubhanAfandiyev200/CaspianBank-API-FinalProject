using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Repository.Data
{
    // İşçi (admin) hesablarını yaradır ki, boş bazada və ya import olunmuş bazada da daxil olmaq mümkün olsun.
    // Hesab artıq varsa heç nə dəyişdirilmir (parol və rol toxunulmaz qalır).
    public static class AdminSeeder
    {
        public static async Task SeedAsync(UserManager<AppUser> userManager,
                                           IEnumerable<SeedAdminSettings> admins,
                                           ILogger logger)
        {
            foreach (var admin in admins)
            {
                if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
                {
                    logger.LogWarning("SeedAdmins: email və ya parol boşdur, sətir atlandı.");
                    continue;
                }

                if (!Roles.All.Contains(admin.Role))
                {
                    logger.LogWarning("SeedAdmins: {Email} üçün rol tanınmadı: {Role}", admin.Email, admin.Role);
                    continue;
                }

                var existing = await userManager.FindByEmailAsync(admin.Email);
                if (existing is not null)
                {
                    continue;
                }

                var user = new AppUser
                {
                    Name = string.IsNullOrWhiteSpace(admin.Name) ? admin.Role : admin.Name,
                    Surname = string.IsNullOrWhiteSpace(admin.Surname) ? "Staff" : admin.Surname,
                    Email = admin.Email,
                    UserName = admin.Email,                 // login email ilədir
                    BirthDay = new DateTime(1990, 1, 1),
                    EmailConfirmed = true                   // işçilər OTP ilə qeydiyyatdan keçmir
                };

                var created = await userManager.CreateAsync(user, admin.Password);
                if (!created.Succeeded)
                {
                    // Parol qaydaya uyğun deyilsə proqram dayanmasın, səbəb log-a yazılsın
                    logger.LogWarning("SeedAdmins: {Email} yaradıla bilmədi: {Errors}",
                        admin.Email, string.Join("; ", created.Errors.Select(e => e.Description)));
                    continue;
                }

                var roleResult = await userManager.AddToRoleAsync(user, admin.Role);
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning("SeedAdmins: {Email} üçün rol verilmədi: {Errors}",
                        admin.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
