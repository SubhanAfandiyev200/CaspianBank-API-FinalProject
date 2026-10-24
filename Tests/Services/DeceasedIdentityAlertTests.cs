using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Security;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // Vəfat etmiş vətəndaşın məlumatı ilə qeydiyyat cəhdi: hesab açılmır, Security işçisinə siqnal gedir (hesab yoxdur, ona görə dondurmaq olmur)
    public class DeceasedIdentityAlertTests
    {
        private static async Task<Customer> DeadCitizenAsync(TestApp app)
        {
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.EnsureOwnBankAsync(db);
            return await TestData.NewCustomerAsync(db, "Vəfat", "Edən", new DateTime(1950, 5, 5), true);
        }

        private static async Task<List<string>> TryAsync(TestApp app, Customer citizen)
        {
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>()
                .CheckIdentityAsync(new CheckIdentityDto { Fin = citizen.Fin, IdCardNumber = citizen.IdCardNumber, Name = citizen.Name, Surname = citizen.Surname });
            return result.Errors.ToList();
        }

        private static async Task<List<SecurityAlert>> AlertsForAsync(TestApp app, Customer citizen)
        {
            return await app.QueryAsync(db => db.SecurityAlerts.AsNoTracking().Where(a => a.Kind == SecurityAlertKind.DeceasedIdentity && a.Description.Contains(citizen.Fin)).ToListAsync());
        }

        [Fact]
        public async Task A_try_with_a_deceased_citizen_raises_one_high_alert_without_an_account()
        {
            var app = await TestApp.SharedAsync();
            var security = await app.NewStaffAsync(Roles.Security);
            var dead = await DeadCitizenAsync(app);

            var errors = await TryAsync(app, dead);
            await TryAsync(app, dead);

            Assert.Equal("This ID record can't be used to open an account. If you think this is a mistake, please contact the bank.", errors[0]);
            var alert = Assert.Single(await AlertsForAsync(app, dead));
            Assert.Null(alert.UserId);
            Assert.Equal(SecurityAlertSeverity.High, alert.Severity);
            Assert.Equal(SecurityAlertStatus.Open, alert.Status);
            Assert.Contains(await app.NotificationsAsync(security.Id), n => n.Title == "Registration attempt with a deceased citizen's ID" && n.Message.Contains(dead.Fin));
        }

        [Fact]
        public async Task The_alert_is_listed_for_the_staff_and_cannot_freeze_an_account()
        {
            var app = await TestApp.SharedAsync();
            var dead = await DeadCitizenAsync(app);
            await TryAsync(app, dead);
            var alert = Assert.Single(await AlertsForAsync(app, dead));

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ISecurityAlertService>();
            var page = await service.GetPageAsync(new SecurityAlertFilterDto { Kind = "DeceasedIdentity", Search = dead.Fin, PageSize = 50 });
            var frozen = await service.ResolveAsync("Tural Hesenov", alert.Id, new ResolveAlertDto { Outcome = "Freeze", Note = "Fake registration" });
            var dismissed = await service.ResolveAsync("Tural Hesenov", alert.Id, new ResolveAlertDto { Outcome = "Dismiss", Note = "Known family request" });

            Assert.True(page.IsSuccess, string.Join(";", page.Errors));
            Assert.Equal(string.Empty, Assert.Single(page.Data!.Items).CustomerId);
            Assert.False(frozen.IsSuccess);
            Assert.Equal("This alert has no account to freeze.", frozen.Errors[0]);
            Assert.True(dismissed.IsSuccess);
            Assert.Equal("Dismissed", dismissed.Data!.Status);
        }

        [Fact]
        public async Task A_living_citizen_raises_no_alert()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.EnsureOwnBankAsync(db);
            var alive = await TestData.NewCustomerAsync(db, "Sağ", "Qalan", new DateTime(1990, 1, 1));

            var errors = await TryAsync(app, alive);

            Assert.Empty(errors);
            Assert.Empty(await AlertsForAsync(app, alive));
        }
    }
}
