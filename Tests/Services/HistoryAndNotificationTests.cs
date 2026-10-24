using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Audit;
using Service.Helpers.DTOs.History;
using Service.Helpers.DTOs.Notifications;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class HistoryAndNotificationTests
    {
        private static async Task<T> WithAsync<T, TService>(TestApp app, Func<TService, Task<T>> action) where TService : notnull
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        private static async Task<Transaction> AddRowAsync(TestApp app, int cardId, DateTime createdUtc, decimal amount, bool income, decimal balanceAfter,
                                                           TransactionType type = TransactionType.TopUp, string description = "Row", string? reference = null)
        {
            return await app.QueryAsync(async db =>
            {
                var row = new Transaction
                {
                    CardId = cardId,
                    Type = type,
                    IsIncome = income,
                    Amount = amount,
                    BalanceAfter = balanceAfter,
                    Description = description,
                    Reference = reference ?? "TST-" + TestData.Next(),
                    CreatedAt = createdUtc
                };
                db.Transactions.Add(row);
                await db.SaveChangesAsync();
                return row;
            });
        }

        // ---------- Tarixçə ----------

        [Fact]
        public async Task History_lists_my_transactions_newest_first_with_totals()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var now = DateTime.UtcNow;
            await AddRowAsync(app, kit.Standard.Id, now.AddMinutes(-30), 100m, true, 100m);
            await AddRowAsync(app, kit.Standard.Id, now.AddMinutes(-20), 30m, false, 70m, TransactionType.BillPayment);
            await AddRowAsync(app, kit.Cashback.Id, now.AddMinutes(-10), 5m, true, 5m, TransactionType.Cashback);

            var page = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto()));

            Assert.Equal(3, page.TotalCount);
            Assert.Equal(new[] { 5m, 30m, 100m }, page.Items.Select(i => i.Amount).ToArray());
            Assert.Equal(105m, page.TotalIn);
            Assert.Equal(30m, page.TotalOut);
            Assert.Equal(1, page.TotalPages);
            Assert.StartsWith("Cashback ••••", page.Items.First().CardLabel);
        }

        [Fact]
        public async Task History_never_shows_another_customers_transactions()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync();
            await AddRowAsync(app, other.Standard.Id, DateTime.UtcNow, 77m, true, 77m);

            var page = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(mine.User.Id, new HistoryFilterDto()));

            Assert.Equal(0, page.TotalCount);
            Assert.Empty(page.Items);
        }

        [Fact]
        public async Task History_filters_by_card_type_direction_and_search()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var now = DateTime.UtcNow;
            await AddRowAsync(app, kit.Standard.Id, now.AddMinutes(-5), 100m, true, 100m, TransactionType.TopUp, "Top up from card 1111");
            await AddRowAsync(app, kit.Standard.Id, now.AddMinutes(-4), 20m, false, 80m, TransactionType.BillPayment, "Azercell payment");
            await AddRowAsync(app, kit.Cashback.Id, now.AddMinutes(-3), 1m, true, 1m, TransactionType.Cashback, "Cashback bonus");

            Task<HistoryPageDto> Get(HistoryFilterDto filter) => WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, filter));

            Assert.Equal(2, (await Get(new HistoryFilterDto { CardId = kit.Standard.Id })).TotalCount);
            Assert.Equal(1, (await Get(new HistoryFilterDto { Type = "billpayment" })).TotalCount);
            Assert.Equal(2, (await Get(new HistoryFilterDto { Direction = "in" })).TotalCount);
            Assert.Equal(1, (await Get(new HistoryFilterDto { Direction = "OUT" })).TotalCount);
            Assert.Equal(1, (await Get(new HistoryFilterDto { Search = "azercell" })).TotalCount);
        }

        [Fact]
        public async Task History_of_somebody_elses_card_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(mine.User.Id, new HistoryFilterDto { CardId = other.Standard.Id })));
        }

        [Fact]
        public async Task History_dates_are_baku_days()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            // 10 mart 21:00 UTC = 11 mart 01:00 Bakı; 10 mart 19:59 UTC = 10 mart 23:59 Bakı
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 3, 10, 21, 0, 0, DateTimeKind.Utc), 1m, true, 1m, description: "after midnight in Baku");
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 3, 10, 19, 59, 0, DateTimeKind.Utc), 2m, true, 3m, description: "before midnight in Baku");

            var on11 = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { From = new DateTime(2026, 3, 11), To = new DateTime(2026, 3, 11) }));
            var on10 = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { From = new DateTime(2026, 3, 10), To = new DateTime(2026, 3, 10) }));

            Assert.Equal("after midnight in Baku", Assert.Single(on11.Items).Description);
            Assert.Equal("before midnight in Baku", Assert.Single(on10.Items).Description);
        }

        [Fact]
        public async Task History_paging_works_and_a_page_past_the_end_shows_the_last_page()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var now = DateTime.UtcNow;
            for (var i = 0; i < 5; i++)
            {
                await AddRowAsync(app, kit.Standard.Id, now.AddMinutes(-i), i + 1, true, i + 1);
            }

            var first = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { Page = 1, PageSize = 2 }));
            var last = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { Page = 3, PageSize = 2 }));
            var beyond = await WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { Page = 99, PageSize = 2 }));

            Assert.Equal(3, first.TotalPages);
            Assert.Equal(2, first.Items.Count());
            Assert.Single(last.Items);
            Assert.Equal(3, beyond.Page);
            Assert.Single(beyond.Items);
        }

        [Fact]
        public async Task History_with_an_invalid_filter_throws_a_bad_request()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            var error = await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<HistoryPageDto, IHistoryService>(app, s => s.GetHistoryAsync(kit.User.Id, new HistoryFilterDto { Direction = "sideways" })));

            Assert.Contains("The direction must be 'in' or 'out'.", error.Errors);
        }

        // ---------- Çıxarış ----------

        [Fact]
        public async Task The_statement_has_opening_closing_and_totals_for_the_range()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(0m, "Aysel", "Mammadova");
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc), 500m, true, 500m, description: "before range");
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc), 200m, false, 300m, TransactionType.BillPayment, "in range 1");
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 3, 6, 10, 0, 0, DateTimeKind.Utc), 50m, true, 350m, description: "in range 2");
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc), 999m, true, 1349m, description: "after range");

            var statement = await WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(kit.User.Id, new StatementFilterDto { CardId = kit.Standard.Id, From = new DateTime(2026, 3, 1), To = new DateTime(2026, 3, 31) }));

            Assert.Equal(500m, statement.OpeningBalance);
            Assert.Equal(350m, statement.ClosingBalance);
            Assert.Equal(50m, statement.TotalIn);
            Assert.Equal(200m, statement.TotalOut);
            Assert.Equal(new[] { "in range 1", "in range 2" }, statement.Rows.Select(r => r.Description).ToArray());
            Assert.Equal("AYSEL MAMMADOVA", statement.HolderName);
            Assert.StartsWith("Standard ••••", statement.CardLabel);
            Assert.Equal(new DateTime(2026, 3, 1), statement.From);
        }

        [Fact]
        public async Task A_statement_without_activity_keeps_the_opening_balance_as_closing()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            await AddRowAsync(app, kit.Standard.Id, new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc), 40m, true, 40m);

            var statement = await WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(kit.User.Id, new StatementFilterDto { CardId = kit.Standard.Id, From = new DateTime(2026, 5, 1), To = new DateTime(2026, 5, 31) }));

            Assert.Equal(40m, statement.OpeningBalance);
            Assert.Equal(40m, statement.ClosingBalance);
            Assert.Empty(statement.Rows);
        }

        [Fact]
        public async Task A_statement_of_a_brand_new_card_opens_at_zero()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            var statement = await WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(kit.User.Id, new StatementFilterDto { CardId = kit.Standard.Id, From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 31) }));

            Assert.Equal(0m, statement.OpeningBalance);
            Assert.Equal(0m, statement.ClosingBalance);
        }

        [Fact]
        public async Task A_statement_is_only_for_my_own_card_and_validates_the_range()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(mine.User.Id, new StatementFilterDto { CardId = other.Standard.Id, From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 31) })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(mine.User.Id, new StatementFilterDto { CardId = mine.Standard.Id, From = new DateTime(2026, 1, 1), To = new DateTime(2027, 6, 1) })));
            await Assert.ThrowsAsync<BadRequestException>(() => WithAsync<StatementDto, IHistoryService>(app, s => s.GetStatementAsync(mine.User.Id, new StatementFilterDto { CardId = mine.Standard.Id })));
        }

        // ---------- Bildirişlər ----------

        [Fact]
        public async Task A_notification_is_stored_published_with_the_unread_count_and_marked_unread()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Card, "First", "Message one", "/App");
            await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Loan, "Second", "Message two");

            var stored = await app.NotificationsAsync(kit.User.Id);
            Assert.Equal(2, stored.Count);
            Assert.All(stored, n => Assert.False(n.IsRead));
            Assert.Equal("/App", stored[0].LinkUrl);
            var published = app.Published.Items.Where(p => p.UserId == kit.User.Id).ToList();
            Assert.Equal(new[] { 1, 2 }, published.Select(p => p.Unread).ToArray());
            Assert.Equal("Second", published.Last().Notification.Title);
            Assert.Equal("Loan", published.Last().Notification.Category);
        }

        [Fact]
        public async Task A_dedupe_key_writes_one_notification_per_user()
        {
            var app = await TestApp.SharedAsync();
            var one = await app.NewKitAsync();
            var two = await app.NewKitAsync();

            using var scope = app.NewScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifications.NotifyAsync(one.User.Id, NotificationCategory.Loan, "Reminder", "text", null, "key-1");
            await notifications.NotifyAsync(one.User.Id, NotificationCategory.Loan, "Reminder", "text", null, "key-1");
            await notifications.NotifyAsync(two.User.Id, NotificationCategory.Loan, "Reminder", "text", null, "key-1");

            Assert.Single(await app.NotificationsAsync(one.User.Id));
            Assert.Single(await app.NotificationsAsync(two.User.Id));
        }

        [Fact]
        public async Task Long_titles_and_messages_are_clipped_with_an_ellipsis()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            await scope.ServiceProvider.GetRequiredService<INotificationService>().NotifyAsync(kit.User.Id, NotificationCategory.Card, new string('t', 300), new string('m', 900));

            var stored = Assert.Single(await app.NotificationsAsync(kit.User.Id));
            Assert.Equal(120, stored.Title.Length);
            Assert.EndsWith("…", stored.Title);
            Assert.Equal(400, stored.Message.Length);
            Assert.EndsWith("…", stored.Message);
        }

        [Fact]
        public async Task Notifying_roles_reaches_everyone_in_those_roles_and_nobody_else()
        {
            var app = await TestApp.SharedAsync();
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            var admin = await app.NewStaffAsync(Roles.Admin);
            var support = await app.NewStaffAsync(Roles.CustomerSupport);
            var customer = await app.NewKitAsync();
            var title = "Role test " + TestData.Next();

            using var scope = app.NewScope();
            await scope.ServiceProvider.GetRequiredService<INotificationService>().NotifyRolesAsync(new[] { Roles.Accountant, Roles.Admin }, NotificationCategory.Staff, title, "for money staff");

            Assert.Contains(await app.NotificationsAsync(accountant.Id), n => n.Title == title);
            Assert.Contains(await app.NotificationsAsync(admin.Id), n => n.Title == title);
            Assert.DoesNotContain(await app.NotificationsAsync(support.Id), n => n.Title == title);
            Assert.DoesNotContain(await app.NotificationsAsync(customer.User.Id), n => n.Title == title);
        }

        [Fact]
        public async Task The_page_shows_my_notifications_newest_first_and_filters_unread()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Card, "Old", "a");
            await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Card, "Middle", "b");
            await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Card, "New", "c");
            await notifications.NotifyAsync(other.User.Id, NotificationCategory.Card, "Foreign", "d");
            var oldId = (await app.NotificationsAsync(kit.User.Id)).First(n => n.Title == "Old").Id;
            await notifications.MarkReadAsync(kit.User.Id, oldId);

            var all = await notifications.GetPageAsync(kit.User.Id, new NotificationFilterDto());
            var unread = await notifications.GetPageAsync(kit.User.Id, new NotificationFilterDto { UnreadOnly = true });

            Assert.Equal(new[] { "New", "Middle", "Old" }, all.Items.Select(i => i.Title).ToArray());
            Assert.Equal(3, all.TotalCount);
            Assert.Equal(2, all.UnreadCount);
            Assert.Equal(new[] { "New", "Middle" }, unread.Items.Select(i => i.Title).ToArray());
            Assert.Equal(2, unread.TotalCount);
        }

        [Fact]
        public async Task Mark_read_works_for_the_owner_only_and_mark_all_clears_the_count()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var stranger = await app.NewKitAsync();

            using var scope = app.NewScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            for (var i = 0; i < 3; i++)
            {
                await notifications.NotifyAsync(kit.User.Id, NotificationCategory.Card, "n" + i, "m");
            }
            var ids = (await app.NotificationsAsync(kit.User.Id)).Select(n => n.Id).ToList();

            Assert.False(await notifications.MarkReadAsync(stranger.User.Id, ids[0]));
            Assert.False(await notifications.MarkReadAsync(kit.User.Id, 999999));
            Assert.True(await notifications.MarkReadAsync(kit.User.Id, ids[0]));
            Assert.True(await notifications.MarkReadAsync(kit.User.Id, ids[0]));        // ikinci dəfə də uğurludur
            Assert.Equal(2, await notifications.GetUnreadCountAsync(kit.User.Id));

            var marked = await notifications.MarkAllReadAsync(kit.User.Id);

            Assert.Equal(2, marked);
            Assert.Equal(0, await notifications.GetUnreadCountAsync(kit.User.Id));
            Assert.All(await app.NotificationsAsync(kit.User.Id), n => Assert.True(n.IsRead));
        }

        [Fact]
        public async Task The_notification_page_validates_the_paging()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            using var scope = app.NewScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await Assert.ThrowsAsync<BadRequestException>(() => notifications.GetPageAsync(kit.User.Id, new NotificationFilterDto { Page = 0 }));
            await Assert.ThrowsAsync<BadRequestException>(() => notifications.GetPageAsync(kit.User.Id, new NotificationFilterDto { PageSize = 51 }));
        }

        [Fact]
        public async Task The_hub_token_belongs_to_the_user()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();

            var token = scope.ServiceProvider.GetRequiredService<INotificationService>().CreateHubToken("user-1");

            Assert.False(string.IsNullOrEmpty(token.Token));
            Assert.True(token.ExpiresAt > DateTime.UtcNow);
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token.Token);
            Assert.Equal("user-1", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        }

        // ---------- Audit ----------

        [Fact]
        public async Task The_audit_log_records_the_actor_name_role_and_trims_long_values()
        {
            var app = await TestApp.SharedAsync();
            var staff = await app.NewStaffAsync(Roles.Admin);
            var marker = "audit-marker-" + TestData.Next();

            using var scope = app.NewScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await audit.LogAsync(staff.Id, AuditActions.SettingChanged, "Setting", marker, new string('d', 500));
            await audit.LogAsync(null, AuditActions.TicketClosed, new string('t', 80), marker + "-system", null);

            var logs = await app.QueryAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.TargetLabel.StartsWith(marker)).OrderBy(a => a.Id).ToListAsync());
            Assert.Equal(2, logs.Count);
            Assert.Equal(staff.Id, logs[0].ActorUserId);
            Assert.Equal("Test User", logs[0].ActorName);
            Assert.Equal("Admin", logs[0].ActorRole);
            Assert.Equal(400, logs[0].Details.Length);
            Assert.Null(logs[1].ActorUserId);
            Assert.Equal("System", logs[1].ActorName);
            Assert.Equal(string.Empty, logs[1].ActorRole);
            Assert.Equal(40, logs[1].TargetType.Length);
            Assert.Equal(string.Empty, logs[1].Details);
        }

        [Fact]
        public async Task An_unknown_actor_is_logged_as_system()
        {
            var app = await TestApp.SharedAsync();
            var marker = "audit-unknown-" + TestData.Next();

            using var scope = app.NewScope();
            await scope.ServiceProvider.GetRequiredService<IAuditService>().LogAsync("no-such-user", AuditActions.SettingChanged, "Setting", marker);

            var log = await app.QueryAsync(db => db.AuditLogs.AsNoTracking().SingleAsync(a => a.TargetLabel == marker));
            Assert.Equal("System", log.ActorName);
        }

        [Fact]
        public async Task The_audit_page_filters_by_text_action_and_baku_dates_and_is_newest_first()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                await app.QueryAsync(async db =>
                {
                    db.AuditLogs.Add(new AuditLog { ActorName = "Nurlan E", ActorRole = "Admin", Action = AuditActions.SettingChanged, TargetType = "Setting", TargetLabel = "Address", Details = "old to new", CreatedAt = new DateTime(2026, 3, 10, 21, 0, 0, DateTimeKind.Utc) });
                    db.AuditLogs.Add(new AuditLog { ActorName = "Tural H", ActorRole = "Security", Action = AuditActions.AccountRestricted, TargetType = "User", TargetLabel = "Aysel", Details = "fraud", CreatedAt = new DateTime(2026, 3, 10, 19, 0, 0, DateTimeKind.Utc) });
                    db.AuditLogs.Add(new AuditLog { ActorName = "Tural H", ActorRole = "Security", Action = AuditActions.AccountRestricted, TargetType = "User", TargetLabel = "100% sure_case", Details = "wild", CreatedAt = new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc) });
                    await db.SaveChangesAsync();
                    return 0;
                });
                using var scope = app.NewScope();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

                var all = await audit.GetPageAsync(new AuditFilterDto());
                Assert.Equal(3, all.Data!.TotalCount);
                Assert.Equal(new[] { "100% sure_case", "Address", "Aysel" }, all.Data.Items.Select(i => i.TargetLabel).ToArray());

                Assert.Equal(2, (await audit.GetPageAsync(new AuditFilterDto { Action = AuditActions.AccountRestricted })).Data!.TotalCount);
                Assert.Equal(1, (await audit.GetPageAsync(new AuditFilterDto { Search = "Nurlan" })).Data!.TotalCount);
                Assert.Equal(1, (await audit.GetPageAsync(new AuditFilterDto { Search = "fraud" })).Data!.TotalCount);
                Assert.Equal(1, (await audit.GetPageAsync(new AuditFilterDto { Search = "100%" })).Data!.TotalCount);
                Assert.Equal(1, (await audit.GetPageAsync(new AuditFilterDto { Search = "sure_c" })).Data!.TotalCount);
                Assert.Equal(0, (await audit.GetPageAsync(new AuditFilterDto { Search = "%%%%" })).Data!.TotalCount);

                // Bakı günü 11 mart: 10 mart 20:00 UTC - 11 mart 20:00 UTC
                var on11 = await audit.GetPageAsync(new AuditFilterDto { From = new DateTime(2026, 3, 11), To = new DateTime(2026, 3, 11) });
                Assert.Equal("Address", Assert.Single(on11.Data!.Items).TargetLabel);
                var on10 = await audit.GetPageAsync(new AuditFilterDto { From = new DateTime(2026, 3, 10), To = new DateTime(2026, 3, 10) });
                Assert.Equal("Aysel", Assert.Single(on10.Data!.Items).TargetLabel);
            }
        }

        [Fact]
        public async Task The_audit_page_rejects_an_unknown_action_a_reversed_range_and_clamps_paging()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

            var action = await audit.GetPageAsync(new AuditFilterDto { Action = "MadeUp" });
            var range = await audit.GetPageAsync(new AuditFilterDto { From = new DateTime(2026, 3, 5), To = new DateTime(2026, 3, 1) });
            var clamped = await audit.GetPageAsync(new AuditFilterDto { Page = -1, PageSize = 100000 });

            Assert.Equal("Choose one of the listed actions.", action.Errors[0]);
            Assert.Equal("The start date can't be after the end date.", range.Errors[0]);
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(100, clamped.Data.PageSize);
        }

        [Fact]
        public void Every_audit_action_constant_is_in_the_All_list()
        {
            var constants = typeof(AuditActions).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToList();

            Assert.NotEmpty(constants);
            Assert.All(constants, c => Assert.Contains(c, AuditActions.All));
            Assert.Equal(constants.Count, AuditActions.All.Distinct().Count());
        }
    }
}
