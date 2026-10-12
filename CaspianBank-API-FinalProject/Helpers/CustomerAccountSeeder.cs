using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // appsettings.Local.json -> "SeedCustomers": qeydiyyatdan keçmiş sınaq müştərilərinin siyahısı
    public class SeedCustomerSettings
    {
        public string Fin { get; set; } = string.Empty;             // reyestrdəki vətəndaşın FİN-i
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public decimal StandardBalance { get; set; }                // Standard karta başlanğıc balans (0 ola bilər)
    }

    // Reyestrdəki bəzi vətəndaşları artıq qeydiyyatdan keçmiş kimi yaradır: onlarla birbaşa giriş etmək olur.
    // Hesab artıq varsa yalnız email fərqli olduqda yenilənir (parol, kart və balansa toxunulmur).
    public static class CustomerAccountSeeder
    {
        private const string EmailPlaceholder = "SENINGMAIL";

        public static async Task SeedAsync(AppDbContext db,
                                           UserManager<AppUser> userManager,
                                           ICardService cardService,
                                           ICardRepository cardRepository,
                                           IEnumerable<SeedCustomerSettings> customers,
                                           ILogger logger)
        {
            foreach (var item in customers)
            {
                if (string.IsNullOrWhiteSpace(item.Email) || string.IsNullOrWhiteSpace(item.Password))
                {
                    logger.LogWarning("SeedCustomers: {Fin} üçün email və ya parol boşdur, sətir atlandı.", item.Fin);
                    continue;
                }

                if (item.Email.Contains(EmailPlaceholder, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("SeedCustomers: {Fin} üçün email hələ dəyişdirilməyib ({Email}), sətir atlandı.", item.Fin, item.Email);
                    continue;
                }

                var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Fin == item.Fin);
                if (customer is null)
                {
                    logger.LogWarning("SeedCustomers: reyestrdə {Fin} tapılmadı, sətir atlandı.", item.Fin);
                    continue;
                }

                var existing = await userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == customer.Id);
                if (existing is not null)
                {
                    await UpdateEmailAsync(userManager, existing, item.Email, logger);
                    continue;
                }

                var user = new AppUser
                {
                    Name = customer.Name,
                    Surname = customer.Surname,
                    BirthDay = customer.BirthDate,
                    FinKod = customer.Fin,
                    CustomerId = customer.Id,
                    Email = item.Email,
                    UserName = item.Email,
                    PhoneNumber = item.Phone,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, item.Password);
                if (!created.Succeeded)
                {
                    logger.LogWarning("SeedCustomers: {Email} yaradıla bilmədi: {Errors}",
                        item.Email, string.Join("; ", created.Errors.Select(e => e.Description)));
                    continue;
                }

                await userManager.AddToRoleAsync(user, Roles.Customer);

                var cards = await cardService.CreateInitialCardsAsync(user.Id);
                if (!cards.IsSuccess)
                {
                    logger.LogWarning("SeedCustomers: {Email} üçün kartlar açılmadı.", item.Email);
                    continue;
                }

                if (item.StandardBalance > 0)
                {
                    // Siyahı izlənmir (AsNoTracking), balansı dəyişmək üçün kart izlənən halda təkrar götürülür
                    var standardInfo = (await cardRepository.GetByUserAsync(user.Id)).FirstOrDefault(c => c.Tier == CardTier.Standard);
                    var standard = standardInfo is null ? null : await cardRepository.GetByIdForUserAsync(standardInfo.Id, user.Id);
                    if (standard is not null)
                    {
                        await cardRepository.TopUpAsync(standard, item.StandardBalance, "Opening balance");
                    }
                }
            }
        }

        private static async Task UpdateEmailAsync(UserManager<AppUser> userManager, AppUser user, string email, ILogger logger)
        {
            if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var taken = await userManager.FindByEmailAsync(email);
            if (taken is not null && taken.Id != user.Id)
            {
                logger.LogWarning("SeedCustomers: {Email} başqa hesabda var, email yenilənmədi.", email);
                return;
            }

            var emailResult = await userManager.SetEmailAsync(user, email);
            var nameResult = await userManager.SetUserNameAsync(user, email);
            if (!emailResult.Succeeded || !nameResult.Succeeded)
            {
                logger.LogWarning("SeedCustomers: {Email} yenilənə bilmədi.", email);
                return;
            }

            // SetEmailAsync təsdiqi sıfırlayır: sınaq hesabı yenə təsdiqli olmalıdır
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }
    }
}
