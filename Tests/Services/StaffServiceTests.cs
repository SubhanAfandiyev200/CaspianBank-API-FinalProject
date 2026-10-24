using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Staff;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class StaffServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public StaffServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IStaffService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IStaffService>());
        }

        private static AddStaffDto NewStaff(string role = Roles.Accountant, string? email = null)
        {
            return new AddStaffDto { Name = "Abdulla", Surname = "Mirzeyev", Email = email ?? TestData.UniqueEmail("staff"), Role = role };
        }

        private async Task<IList<string>> RolesOfAsync(string userId)
        {
            using var scope = _app.NewScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await manager.FindByIdAsync(userId);
            return await manager.GetRolesAsync(user!);
        }

        // ---------- Siyahı ----------

        [Fact]
        public async Task The_list_has_staff_only_the_super_admin_first_and_the_assignable_roles()
        {
            var superAdmin = await _app.NewStaffAsync(Roles.SuperAdmin);
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var customer = await _app.NewKitAsync();

            var list = await WithAsync(s => s.GetListAsync());

            Assert.Contains(list.Items, i => i.Id == superAdmin.Id && i.IsSuperAdmin);
            var firstNonSuper = list.Items.FindIndex(i => !i.IsSuperAdmin);
            Assert.True(firstNonSuper < 0 || list.Items.Skip(firstNonSuper).All(i => !i.IsSuperAdmin));      // super adminlər siyahının başında
            Assert.Contains(list.Items, i => i.Id == support.Id && i.Role == Roles.CustomerSupport && i.IsActive);
            Assert.DoesNotContain(list.Items, i => i.Id == customer.User.Id);
            Assert.Equal(new[] { Roles.Admin, Roles.Accountant, Roles.CustomerSupport, Roles.Security }, list.AssignableRoles);
        }

        // ---------- Əlavə etmə ----------

        [Fact]
        public async Task Adding_staff_creates_a_confirmed_account_with_the_role_and_sends_an_invite()
        {
            var model = NewStaff(Roles.Security);
            model.Name = "  Tural  ";

            var result = await WithAsync(s => s.AddAsync(model));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var staff = result.Data!;
            Assert.Equal("Tural", staff.Name);
            Assert.Equal(Roles.Security, staff.Role);
            Assert.True(staff.IsActive);
            Assert.False(staff.IsSuperAdmin);
            Assert.Equal(new[] { Roles.Security }, await RolesOfAsync(staff.Id));
            var user = await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == staff.Id));
            Assert.True(user.EmailConfirmed);
            Assert.Null(user.CustomerId);

            var mail = app_Mail(model.Email);
            Assert.Equal("Your Caspian Bank staff account", mail.Subject);
            Assert.Contains("Hello Tural", mail.Body);
            Assert.StartsWith("https://frontend.test/Account/ResetPassword?email=", mail.Link);
            Assert.Equal(mail.Link, staff.InviteLink);
        }

        private SentEmail app_Mail(string email)
        {
            return _app.Emails.LastTo(email)!;
        }

        [Fact]
        public async Task The_invited_staff_member_sets_a_password_with_the_link_and_logs_in()
        {
            var model = NewStaff(Roles.Admin);
            var added = await WithAsync(s => s.AddAsync(model));
            var link = new Uri(added.Data!.InviteLink!);
            var query = System.Web.HttpUtility.ParseQueryString(link.Query);

            using var scope = _app.NewScope();
            var reset = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ResetPasswordAsync(new ResetPasswordDto { Email = query["email"]!, Token = query["token"]!, NewPassword = "Staff#Pass1" });
            var login = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = model.Email, Password = "Staff#Pass1" });

            Assert.True(reset.IsSuccess, string.Join(";", reset.Errors));
            Assert.True(login.IsSuccess, string.Join(";", login.Errors));
            Assert.Equal(new[] { Roles.Admin }, login.Roles);
        }

        [Fact]
        public async Task Nobody_can_log_in_with_a_guessed_temporary_password()
        {
            var model = NewStaff();
            await WithAsync(s => s.AddAsync(model));

            using var scope = _app.NewScope();
            var login = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = model.Email, Password = "Salam123!" });

            Assert.False(login.IsSuccess);
        }

        [Fact]
        public async Task Adding_staff_checks_the_input_and_the_email_must_be_new()
        {
            var existing = await _app.NewStaffAsync(Roles.Admin);

            var invalidRole = await WithAsync(s => s.AddAsync(NewStaff(Roles.SuperAdmin)));
            var invalidEmail = await WithAsync(s => s.AddAsync(NewStaff(Roles.Admin, "not-an-email")));
            var duplicate = await WithAsync(s => s.AddAsync(NewStaff(Roles.Admin, existing.Email)));
            var duplicateInOtherCase = await WithAsync(s => s.AddAsync(NewStaff(Roles.Admin, existing.Email!.ToUpperInvariant())));

            Assert.False(invalidRole.IsSuccess);
            Assert.False(invalidEmail.IsSuccess);
            Assert.Equal("An account with this email already exists.", duplicate.Errors[0]);
            Assert.False(duplicateInOtherCase.IsSuccess);
        }

        [Fact]
        public async Task The_account_is_created_even_when_the_invite_email_fails()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                app.Emails.ShouldFail = true;
                var model = NewStaff();
                using var scope = app.NewScope();

                var result = await scope.ServiceProvider.GetRequiredService<IStaffService>().AddAsync(model);

                Assert.True(result.IsSuccess);
                Assert.True(await app.QueryAsync(db => db.Users.AnyAsync(u => u.Email == model.Email)));
            }
        }

        // ---------- Rol dəyişmə ----------

        [Fact]
        public async Task Changing_the_role_replaces_the_old_role()
        {
            var actor = await _app.NewStaffAsync(Roles.SuperAdmin);
            var staff = await _app.NewStaffAsync(Roles.Accountant);

            var result = await WithAsync(s => s.ChangeRoleAsync(actor.Id, staff.Id, new ChangeStaffRoleDto { Role = Roles.Security }));

            Assert.True(result.IsSuccess);
            Assert.Equal(Roles.Security, result.Data!.Role);
            Assert.Equal(new[] { Roles.Security }, await RolesOfAsync(staff.Id));
        }

        [Fact]
        public async Task Setting_the_same_role_again_changes_nothing_and_succeeds()
        {
            var actor = await _app.NewStaffAsync(Roles.SuperAdmin);
            var staff = await _app.NewStaffAsync(Roles.Admin);

            var result = await WithAsync(s => s.ChangeRoleAsync(actor.Id, staff.Id, new ChangeStaffRoleDto { Role = Roles.Admin }));

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { Roles.Admin }, await RolesOfAsync(staff.Id));
        }

        [Fact]
        public async Task The_role_cannot_be_changed_for_oneself_the_super_admin_customers_or_unknown_ids()
        {
            var actor = await _app.NewStaffAsync(Roles.SuperAdmin);
            var other = await _app.NewStaffAsync(Roles.SuperAdmin);
            var staff = await _app.NewStaffAsync(Roles.Admin);
            var customer = await _app.NewKitAsync();
            var model = new ChangeStaffRoleDto { Role = Roles.Security };

            var self = await WithAsync(s => s.ChangeRoleAsync(staff.Id, staff.Id, model));
            var superAdmin = await WithAsync(s => s.ChangeRoleAsync(actor.Id, other.Id, model));
            var onCustomer = await WithAsync(s => s.ChangeRoleAsync(actor.Id, customer.User.Id, model));
            var unknown = await WithAsync(s => s.ChangeRoleAsync(actor.Id, "nobody", model));
            var invalid = await WithAsync(s => s.ChangeRoleAsync(actor.Id, staff.Id, new ChangeStaffRoleDto { Role = Roles.SuperAdmin }));

            Assert.Equal("You can't change your own account.", self.Errors[0]);
            Assert.Equal("The super admin account can't be changed here.", superAdmin.Errors[0]);
            Assert.True(onCustomer.IsNotFound);
            Assert.True(unknown.IsNotFound);
            Assert.False(invalid.IsSuccess);
            Assert.Equal(new[] { Roles.Admin }, await RolesOfAsync(staff.Id));
            Assert.Equal(new[] { Roles.Customer }, await RolesOfAsync(customer.User.Id));
        }

        // ---------- Aktiv / deaktiv ----------

        [Fact]
        public async Task A_deactivated_staff_member_cannot_log_in_until_reactivated()
        {
            var actor = await _app.NewStaffAsync(Roles.SuperAdmin);
            var staff = await _app.NewStaffAsync(Roles.Accountant);

            var off = await WithAsync(s => s.SetActiveAsync(actor.Id, "Subhan Efendiyev", staff.Id, false));
            Assert.True(off.IsSuccess);
            Assert.False(off.Data!.IsActive);
            var user = await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == staff.Id));
            Assert.True(user.IsRestricted);
            Assert.Equal("Deactivated by the super admin", user.RestrictionReason);
            Assert.Equal("Subhan Efendiyev", user.RestrictedBy);

            using (var scope = _app.NewScope())
            {
                var blocked = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = staff.Email!, Password = TestData.Password });
                Assert.Equal("Your account is restricted. Please contact support.", blocked.Errors[0]);
            }

            var on = await WithAsync(s => s.SetActiveAsync(actor.Id, "Subhan Efendiyev", staff.Id, true));
            Assert.True(on.Data!.IsActive);
            using var again = _app.NewScope();
            var allowed = await again.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = staff.Email!, Password = TestData.Password });
            Assert.True(allowed.IsSuccess);
            var restored = await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == staff.Id));
            Assert.Null(restored.RestrictionReason);
            Assert.Null(restored.RestrictedAt);
            Assert.Null(restored.RestrictedBy);
        }

        [Fact]
        public async Task Deactivation_cannot_target_oneself_the_super_admin_customers_or_unknown_ids()
        {
            var actor = await _app.NewStaffAsync(Roles.SuperAdmin);
            var other = await _app.NewStaffAsync(Roles.SuperAdmin);
            var customer = await _app.NewKitAsync();

            var admin = await _app.NewStaffAsync(Roles.Admin);
            Assert.Equal("You can't change your own account.", (await WithAsync(s => s.SetActiveAsync(admin.Id, "A", admin.Id, false))).Errors[0]);
            Assert.Equal("The super admin account can't be changed here.", (await WithAsync(s => s.SetActiveAsync(actor.Id, "A", other.Id, false))).Errors[0]);
            Assert.True((await WithAsync(s => s.SetActiveAsync(actor.Id, "A", customer.User.Id, false))).IsNotFound);
            Assert.True((await WithAsync(s => s.SetActiveAsync(actor.Id, "A", "nobody", false))).IsNotFound);
            Assert.False(await _app.QueryAsync(db => db.Users.AsNoTracking().Where(u => u.Id == customer.User.Id).Select(u => u.IsRestricted).FirstAsync()));
        }
    }
}
