using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Security;
using Service.Helpers.DTOs.Transfers;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class SecurityServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public SecurityServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T, TService>(Func<TService, Task<T>> action) where TService : notnull
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        private async Task<List<SecurityAlert>> AlertsAsync(string userId)
        {
            return await _app.QueryAsync(db => db.SecurityAlerts.AsNoTracking().Where(a => a.UserId == userId).OrderBy(a => a.Id).ToListAsync());
        }

        private async Task<TransferReceiptDto> SendAsync(CustomerKit sender, CustomerKit recipient, decimal amount, int? fromCard = null)
        {
            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<TransferReceiptDto>, ITransferService>(s => s.TransferAsync(sender.User.Id, new TransferDto
            {
                FromCardId = fromCard ?? sender.Standard.Id,
                ToCardNumber = recipient.Standard.CardNumber,
                Amount = amount,
                RequestId = Guid.NewGuid().ToString()
            }));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            return result.Data!;
        }

        // ---------- Böyük köçürmə ----------

        [Fact]
        public async Task A_transfer_of_3000_or_more_raises_a_medium_alert_and_tells_the_security_staff()
        {
            var security = await _app.NewStaffAsync(Roles.Security);
            var accountant = await _app.NewStaffAsync(Roles.Accountant);
            var sender = await _app.NewKitAsync(20000m, "Nurlan", "Efendiyev");
            var recipient = await _app.NewKitAsync();

            var receipt = await SendAsync(sender, recipient, 3000m);

            var alert = Assert.Single(await AlertsAsync(sender.User.Id));
            Assert.Equal(SecurityAlertKind.LargeTransfer, alert.Kind);
            Assert.Equal(SecurityAlertSeverity.Medium, alert.Severity);
            Assert.Equal(SecurityAlertStatus.Open, alert.Status);
            Assert.Equal(3000m, alert.Amount);
            Assert.Equal(receipt.Reference, alert.Reference);
            var notice = Assert.Single(await _app.NotificationsAsync(security.Id), n => n.Title == "Suspicious activity" && n.Message.Contains("Nurlan Efendiyev"));
            Assert.Equal("/Admin/Security", notice.LinkUrl);
            Assert.Equal(NotificationCategory.Staff, notice.Category);
            Assert.DoesNotContain(await _app.NotificationsAsync(accountant.Id), n => n.Title.StartsWith("Suspicious"));
        }

        [Fact]
        public async Task A_transfer_of_8000_or_more_is_a_high_alert()
        {
            var admin = await _app.NewStaffAsync(Roles.Admin);
            var sender = await _app.NewKitAsync(50000m);
            var recipient = await _app.NewKitAsync();

            await SendAsync(sender, recipient, 8000m);

            var alert = Assert.Single(await AlertsAsync(sender.User.Id), a => a.Kind == SecurityAlertKind.LargeTransfer);
            Assert.Equal(SecurityAlertSeverity.High, alert.Severity);
            Assert.Contains(await _app.NotificationsAsync(admin.Id), n => n.Title == "Suspicious activity (high)");
        }

        [Fact]
        public async Task A_transfer_below_3000_raises_nothing_for_an_old_account()
        {
            var sender = await _app.NewKitAsync(20000m);
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == sender.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, DateTime.UtcNow.AddDays(-30)));
                return 0;
            });
            var recipient = await _app.NewKitAsync();

            await SendAsync(sender, recipient, 2999m);

            Assert.Empty(await AlertsAsync(sender.User.Id));
        }

        [Fact]
        public async Task Transfers_between_own_cards_are_never_suspicious()
        {
            var kit = await _app.NewKitAsync(50000m);
            var silver = await _app.NewCardAsync(kit.User.Id, CardTier.Silver);

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<TransferReceiptDto>, ITransferService>(s => s.TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 9000m, RequestId = Guid.NewGuid().ToString() }));

            Assert.True(result.IsSuccess);
            Assert.Empty(await AlertsAsync(kit.User.Id));
        }

        [Fact]
        public async Task The_same_reference_raises_one_alert_and_one_notification()
        {
            var security = await _app.NewStaffAsync(Roles.Security);
            var kit = await _app.NewKitAsync();

            await WithAsync<int, ISecurityMonitor>(async m => { await m.EvaluateTransferAsync(kit.User.Id, 5000m, "TR-DUPLICATE1"); return 0; });
            await WithAsync<int, ISecurityMonitor>(async m => { await m.EvaluateTransferAsync(kit.User.Id, 5000m, "TR-DUPLICATE1"); return 0; });

            Assert.Single(await AlertsAsync(kit.User.Id));
            Assert.Single(await _app.NotificationsAsync(security.Id), n => n.Message.Contains("5,000.00 AZN in one transfer"));
        }

        // ---------- Tez-tez köçürmə, gündəlik cəm, yeni hesab ----------

        [Fact]
        public async Task Five_transfers_to_other_people_within_ten_minutes_raise_a_rapid_alert_once()
        {
            var sender = await _app.NewKitAsync(5000m);
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == sender.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, DateTime.UtcNow.AddDays(-30)));
                return 0;
            });
            var recipient = await _app.NewKitAsync();

            for (var i = 0; i < 4; i++)
            {
                await SendAsync(sender, recipient, 10m);
            }
            Assert.DoesNotContain(await AlertsAsync(sender.User.Id), a => a.Kind == SecurityAlertKind.RapidTransfers);

            await SendAsync(sender, recipient, 10m);
            await SendAsync(sender, recipient, 10m);

            var rapid = Assert.Single(await AlertsAsync(sender.User.Id), a => a.Kind == SecurityAlertKind.RapidTransfers);
            Assert.Equal(SecurityAlertSeverity.Medium, rapid.Severity);
            Assert.Contains("transfers to other people in 10 minutes", rapid.Description);
        }

        [Fact]
        public async Task Transfers_adding_up_to_15000_in_a_day_raise_a_high_daily_alert()
        {
            var sender = await _app.NewKitAsync(0m);
            var gold = await _app.NewCardAsync(sender.User.Id, CardTier.Gold, 30000m);
            var recipient = await _app.NewKitAsync();

            await SendAsync(sender, recipient, 5000m, gold.Id);
            await SendAsync(sender, recipient, 5000m, gold.Id);
            Assert.DoesNotContain(await AlertsAsync(sender.User.Id), a => a.Kind == SecurityAlertKind.DailyVolume);
            await SendAsync(sender, recipient, 5000m, gold.Id);

            var daily = Assert.Single(await AlertsAsync(sender.User.Id), a => a.Kind == SecurityAlertKind.DailyVolume);
            Assert.Equal(SecurityAlertSeverity.High, daily.Severity);
            Assert.Equal(15000m, daily.Amount);
        }

        [Fact]
        public async Task A_new_account_sending_1000_or_more_raises_an_alert_but_an_old_account_does_not()
        {
            var fresh = await _app.NewKitAsync(5000m);
            var old = await _app.NewKitAsync(5000m);
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == old.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, DateTime.UtcNow.AddDays(-8)));
                return 0;
            });
            var recipient = await _app.NewKitAsync();

            await SendAsync(fresh, recipient, 1000m);
            await SendAsync(old, recipient, 1000m);

            var alert = Assert.Single(await AlertsAsync(fresh.User.Id));
            Assert.Equal(SecurityAlertKind.NewAccountOutflow, alert.Kind);
            Assert.Equal(SecurityAlertSeverity.Medium, alert.Severity);
            Assert.Empty(await AlertsAsync(old.User.Id));
        }

        [Fact]
        public async Task A_new_account_sending_999_raises_nothing()
        {
            var fresh = await _app.NewKitAsync(5000m);
            var recipient = await _app.NewKitAsync();

            await SendAsync(fresh, recipient, 999m);

            Assert.Empty(await AlertsAsync(fresh.User.Id));
        }

        [Fact]
        public async Task A_large_transfer_from_a_new_account_gets_only_the_large_transfer_alert()
        {
            var fresh = await _app.NewKitAsync(20000m);
            var recipient = await _app.NewKitAsync();

            await SendAsync(fresh, recipient, 4000m);

            var alerts = await AlertsAsync(fresh.User.Id);
            Assert.Single(alerts);
            Assert.Equal(SecurityAlertKind.LargeTransfer, alerts[0].Kind);
        }

        // ---------- Hesabın bloklanması ----------

        [Fact]
        public async Task A_locked_customer_account_raises_a_lockout_alert_once_per_hour()
        {
            var kit = await _app.NewKitAsync();

            using (var scope = _app.NewScope())
            {
                var account = scope.ServiceProvider.GetRequiredService<IAccountService>();
                for (var i = 0; i < 6; i++)
                {
                    await account.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Wrong123!" });
                }
            }
            await WithAsync<int, ISecurityMonitor>(async m => { await m.OnAccountLockedAsync(kit.User.Id); return 0; });

            var alert = Assert.Single(await AlertsAsync(kit.User.Id));
            Assert.Equal(SecurityAlertKind.AccountLockout, alert.Kind);
            Assert.Equal(SecurityAlertSeverity.Medium, alert.Severity);
        }

        [Fact]
        public async Task A_locked_staff_account_raises_no_alert_and_an_unknown_user_is_ignored()
        {
            var staff = await _app.NewStaffAsync(Roles.Accountant);

            await WithAsync<int, ISecurityMonitor>(async m => { await m.OnAccountLockedAsync(staff.Id); return 0; });
            await WithAsync<int, ISecurityMonitor>(async m => { await m.OnAccountLockedAsync("nobody"); return 0; });

            Assert.Empty(await AlertsAsync(staff.Id));
        }

        // ---------- Siqnalların siyahısı ----------

        private async Task<SecurityAlert> AddAlertAsync(CustomerKit kit, SecurityAlertKind kind = SecurityAlertKind.LargeTransfer, SecurityAlertSeverity severity = SecurityAlertSeverity.Medium,
                                                         SecurityAlertStatus status = SecurityAlertStatus.Open, string? reference = null, string description = "test alert")
        {
            return await _app.QueryAsync(async db =>
            {
                var alert = new SecurityAlert
                {
                    UserId = kit.User.Id,
                    Kind = kind,
                    Severity = severity,
                    Status = status,
                    Reference = reference ?? "REF-" + TestData.Next(),
                    Description = description,
                    Amount = 100m,
                    ResolvedAt = status == SecurityAlertStatus.Open ? null : DateTime.UtcNow
                };
                db.SecurityAlerts.Add(alert);
                await db.SaveChangesAsync();
                return alert;
            });
        }

        [Fact]
        public async Task The_list_filters_by_status_severity_kind_and_search_and_counts_open_alerts()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(0m, "Zaur", "Alertov");
                var other = await app.NewKitAsync(0m, "Other", "Person");
                async Task<SecurityAlert> Add(CustomerKit k, SecurityAlertKind kind, SecurityAlertSeverity sev, SecurityAlertStatus st)
                {
                    return await app.QueryAsync(async db =>
                    {
                        var a = new SecurityAlert { UserId = k.User.Id, Kind = kind, Severity = sev, Status = st, Reference = "R" + TestData.Next(), Description = "d", ResolvedAt = st == SecurityAlertStatus.Open ? null : DateTime.UtcNow };
                        db.SecurityAlerts.Add(a);
                        await db.SaveChangesAsync();
                        return a;
                    });
                }
                await Add(kit, SecurityAlertKind.LargeTransfer, SecurityAlertSeverity.High, SecurityAlertStatus.Open);
                await Add(kit, SecurityAlertKind.RapidTransfers, SecurityAlertSeverity.Medium, SecurityAlertStatus.Open);
                await Add(kit, SecurityAlertKind.DailyVolume, SecurityAlertSeverity.High, SecurityAlertStatus.Dismissed);
                await Add(other, SecurityAlertKind.AccountLockout, SecurityAlertSeverity.Medium, SecurityAlertStatus.Frozen);

                using var scope = app.NewScope();
                var service = scope.ServiceProvider.GetRequiredService<ISecurityAlertService>();

                var all = await service.GetPageAsync(new SecurityAlertFilterDto());
                Assert.Equal(4, all.Data!.Total);
                Assert.Equal(2, all.Data.OpenCount);
                Assert.Equal(1, all.Data.OpenHighCount);
                Assert.Equal(2, all.Data.ResolvedThisWeek);
                // açıq siqnallar əvvəl, onlarda yüksək əvvəl
                Assert.Equal(new[] { "Open", "Open" }, all.Data.Items.Take(2).Select(a => a.Status).ToArray());
                Assert.Equal("High", all.Data.Items[0].Severity);

                Assert.Equal(2, (await service.GetPageAsync(new SecurityAlertFilterDto { Status = "Open" })).Data!.Total);
                Assert.Equal(2, (await service.GetPageAsync(new SecurityAlertFilterDto { Severity = "high" })).Data!.Total);
                Assert.Equal(1, (await service.GetPageAsync(new SecurityAlertFilterDto { Kind = "AccountLockout" })).Data!.Total);
                Assert.Equal(3, (await service.GetPageAsync(new SecurityAlertFilterDto { Search = "Alertov" })).Data!.Total);
                Assert.Equal(1, (await service.GetPageAsync(new SecurityAlertFilterDto { PageSize = 1, Page = 2 })).Data!.Items.Count);
            }
        }

        [Fact]
        public async Task The_list_rejects_unknown_filters_and_clamps_paging()
        {
            using var scope = _app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ISecurityAlertService>();

            Assert.Equal("Unknown status.", (await service.GetPageAsync(new SecurityAlertFilterDto { Status = "Gone" })).Errors[0]);
            Assert.Equal("Unknown severity.", (await service.GetPageAsync(new SecurityAlertFilterDto { Severity = "Huge" })).Errors[0]);
            Assert.Equal("Unknown alert type.", (await service.GetPageAsync(new SecurityAlertFilterDto { Kind = "Weird" })).Errors[0]);
            var clamped = await service.GetPageAsync(new SecurityAlertFilterDto { Page = -1, PageSize = 100000 });
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(100, clamped.Data.PageSize);
        }

        // ---------- Qərar ----------

        [Fact]
        public async Task Dismissing_an_alert_records_who_and_when_without_touching_the_account()
        {
            var kit = await _app.NewKitAsync();
            var alert = await AddAlertAsync(kit);

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("Tural Hesenov", alert.Id, new ResolveAlertDto { Outcome = "dismiss", Note = "  known customer  " }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Dismissed", result.Data!.Status);
            Assert.Equal("Tural Hesenov", result.Data.ResolvedByName);
            Assert.Equal("known customer", result.Data.ResolutionNote);
            Assert.NotNull(result.Data.ResolvedAt);
            Assert.False(result.Data.AccountRestricted);
            Assert.False(await _app.QueryAsync(db => db.Users.AsNoTracking().Where(u => u.Id == kit.User.Id).Select(u => u.IsRestricted).FirstAsync()));
        }

        [Fact]
        public async Task Freezing_restricts_the_account_with_the_reason_and_closes_the_alert()
        {
            var kit = await _app.NewKitAsync();
            var alert = await AddAlertAsync(kit);

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("Tural Hesenov", alert.Id, new ResolveAlertDto { Outcome = "Freeze", Note = "Looks like fraud" }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("Frozen", result.Data!.Status);
            Assert.True(result.Data.AccountRestricted);
            var user = await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == kit.User.Id));
            Assert.True(user.IsRestricted);
            Assert.Equal("Security: Looks like fraud", user.RestrictionReason);
            Assert.Equal("Tural Hesenov", user.RestrictedBy);
        }

        [Fact]
        public async Task Freezing_an_already_restricted_account_still_closes_the_alert()
        {
            var kit = await _app.NewKitAsync();
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true).SetProperty(u => u.RestrictionReason, "earlier"));
                return 0;
            });
            var alert = await AddAlertAsync(kit);

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("Staff", alert.Id, new ResolveAlertDto { Outcome = "Freeze", Note = "frozen too" }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Frozen", result.Data!.Status);
            Assert.Equal("earlier", (await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == kit.User.Id))).RestrictionReason);
        }

        [Fact]
        public async Task Resolve_checks_the_outcome_the_note_and_the_alert()
        {
            var kit = await _app.NewKitAsync();
            var alert = await AddAlertAsync(kit);

            async Task<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>> Resolve(int id, ResolveAlertDto model)
            {
                return await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("Staff", id, model));
            }

            Assert.Equal("Choose what to do with this alert.", (await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Maybe" })).Errors[0]);
            Assert.Equal("The note can be at most 300 characters.", (await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Dismiss", Note = new string('n', 301) })).Errors[0]);
            Assert.Equal("Write why the account is frozen (at least 5 characters).", (await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Freeze", Note = "abc" })).Errors[0]);
            Assert.Equal("Write why the account is frozen (at least 5 characters).", (await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Freeze" })).Errors[0]);
            Assert.True((await Resolve(999999, new ResolveAlertDto { Outcome = "Dismiss" })).IsNotFound);

            Assert.True((await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Dismiss" })).IsSuccess);
            Assert.Equal("This alert was already handled.", (await Resolve(alert.Id, new ResolveAlertDto { Outcome = "Dismiss" })).Errors[0]);
        }

        [Fact]
        public async Task A_blank_staff_name_is_recorded_as_staff()
        {
            var kit = await _app.NewKitAsync();
            var alert = await AddAlertAsync(kit);

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("  ", alert.Id, new ResolveAlertDto { Outcome = "Dismiss" }));

            Assert.Equal("Staff", result.Data!.ResolvedByName);
        }

        [Fact]
        public async Task A_staff_account_cannot_be_frozen_from_an_alert()
        {
            var staff = await _app.NewStaffAsync(Roles.Accountant);
            var alertId = await _app.QueryAsync(async db =>
            {
                var alert = new SecurityAlert { UserId = staff.Id, Kind = SecurityAlertKind.LargeTransfer, Severity = SecurityAlertSeverity.Medium, Reference = "STAFF-" + TestData.Next(), Description = "x" };
                db.SecurityAlerts.Add(alert);
                await db.SaveChangesAsync();
                return alert.Id;
            });

            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<SecurityAlertDto>, ISecurityAlertService>(s => s.ResolveAsync("Staff", alertId, new ResolveAlertDto { Outcome = "Freeze", Note = "freeze them" }));

            Assert.False(result.IsSuccess);
            Assert.Equal("The account can't be frozen here (it is not a customer account).", result.Errors[0]);
        }
    }
}
