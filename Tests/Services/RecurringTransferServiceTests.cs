using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Domain.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Transfers;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class RecurringTransferServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public RecurringTransferServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IRecurringTransferService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IRecurringTransferService>());
        }

        private static DateTime Today()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow);
        }

        private static string PreviousPeriod()
        {
            var today = Today();
            return RecurringTransferRules.PeriodOf(new DateTime(today.Year, today.Month, 1).AddMonths(-1));
        }

        private static CreateRecurringTransferDto ToOtherCard(CustomerKit sender, CustomerKit recipient, decimal amount = 100m, int day = 1)
        {
            return new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = amount, DayOfMonth = day };
        }

        // Köçürməni "bu ay hələ edilməyib" vəziyyətinə salır (gün 1: bu gün həmişə vaxtı çatmış olur)
        private async Task MakeDueAsync(int id)
        {
            await _app.QueryAsync(async db =>
            {
                await db.RecurringTransfers.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastPeriod, PreviousPeriod()));
                return 0;
            });
        }

        private async Task<RecurringTransfer> LoadAsync(int id)
        {
            return await _app.QueryAsync(db => db.RecurringTransfers.AsNoTracking().FirstAsync(r => r.Id == id));
        }

        // ---------- Yaratma ----------

        [Fact]
        public async Task Create_saves_the_monthly_transfer_with_a_readable_recipient_label()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync(0m, "Aysel", "Məmmədova");

            var result = await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 150m, 5)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var item = result.Data!;
            Assert.Equal(150m, item.Amount);
            Assert.Equal(5, item.DayOfMonth);
            Assert.True(item.IsActive);
            Assert.Equal("Aysel M. · •••• " + recipient.Standard.CardNumber[^4..], item.ToLabel);
            Assert.Equal("Standard •••• " + sender.Standard.CardNumber[^4..], item.FromLabel);
            Assert.Null(item.LastError);
            Assert.Equal(1000m, await _app.BalanceAsync(sender.Standard.Id));     // yaradanda pul köçmür
        }

        [Fact]
        public async Task Create_to_an_own_card_is_labelled_with_the_card()
        {
            var kit = await _app.NewKitAsync(1000m);
            var silver = await _app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            var result = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringTransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 50m, DayOfMonth = 10, Note = "  savings  " }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Silver •••• " + silver.CardNumber[^4..], result.Data!.ToLabel);
            Assert.Equal("savings", result.Data.Note);
        }

        [Fact]
        public async Task The_first_run_date_is_the_next_chosen_day()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var today = Today();

            var created = await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 10m, 5)));

            var expected = today.Day >= 5 ? new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(4) : new DateTime(today.Year, today.Month, 5);
            Assert.Equal(expected, created.Data!.NextRunDate);
        }

        [Theory]
        [InlineData(0, 5, "Enter an amount between 0.01 and 100,000 AZN with at most two decimal places.")]
        [InlineData(100000.01, 5, "Enter an amount between 0.01 and 100,000 AZN with at most two decimal places.")]
        [InlineData(10.005, 5, "Enter an amount between 0.01 and 100,000 AZN with at most two decimal places.")]
        [InlineData(10, 0, "Choose a day between 1 and 28.")]
        [InlineData(10, 29, "Choose a day between 1 and 28.")]
        public async Task Create_validates_amount_and_day(double amount, int day, string message)
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();

            var result = await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, (decimal)amount, day)));

            Assert.False(result.IsSuccess);
            Assert.Equal(message, result.Errors[0]);
        }

        [Fact]
        public async Task Create_accepts_the_boundary_amounts_and_days()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();

            Assert.True((await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 0.01m, 1)))).IsSuccess);
            Assert.True((await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100000m, 28)))).IsSuccess);
        }

        [Fact]
        public async Task Create_needs_exactly_one_recipient_a_card_and_a_short_note()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var silver = await _app.NewCardAsync(sender.User.Id, CardTier.Silver);

            var none = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, Amount = 10m, DayOfMonth = 5 }));
            var both = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, ToCardId = silver.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 10m, DayOfMonth = 5 }));
            var noCard = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = 0, ToCardNumber = recipient.Standard.CardNumber, Amount = 10m, DayOfMonth = 5 }));
            var longNote = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 10m, DayOfMonth = 5, Note = new string('n', 141) }));

            Assert.Equal("Choose one of your cards or enter the recipient's card number.", none.Errors[0]);
            Assert.Equal("Choose one of your cards or enter the recipient's card number.", both.Errors[0]);
            Assert.Equal("Choose the card to send from.", noCard.Errors[0]);
            Assert.Equal("The note can be at most 140 characters.", longNote.Errors[0]);
        }

        [Fact]
        public async Task Create_checks_the_recipient_like_a_normal_transfer()
        {
            var sender = await _app.NewKitAsync(1000m);
            var stranger = await _app.NewKitAsync();

            var unknown = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, ToCardNumber = "1111222233334444", Amount = 10m, DayOfMonth = 5 }));
            var cashback = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = sender.Standard.Id, ToCardId = sender.Cashback.Id, Amount = 10m, DayOfMonth = 5 }));
            var foreignSource = await WithAsync(s => s.CreateAsync(sender.User.Id, new CreateRecurringTransferDto { FromCardId = stranger.Standard.Id, ToCardId = sender.Standard.Id, Amount = 10m, DayOfMonth = 5 }));

            Assert.Equal("We couldn't find a card with that number.", unknown.Errors[0]);
            Assert.Contains("Cashback card only collects cashback", cashback.Errors[0]);
            Assert.Equal("Choose one of your own cards to send from.", foreignSource.Errors[0]);
        }

        [Fact]
        public async Task Create_is_refused_for_a_restricted_account_and_after_ten_transfers()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            for (var i = 0; i < 10; i++)
            {
                Assert.True((await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 1m, 5)))).IsSuccess);
            }

            var eleventh = await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 1m, 5)));
            var ghost = await WithAsync(s => s.CreateAsync("nobody", ToOtherCard(sender, recipient)));

            Assert.Equal("You can have at most 10 monthly transfers. Delete one to add another.", eleventh.Errors[0]);
            Assert.True(ghost.IsNotFound);

            var restricted = await _app.NewKitAsync(1000m);
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == restricted.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var blocked = await WithAsync(s => s.CreateAsync(restricted.User.Id, ToOtherCard(restricted, recipient)));
            Assert.Equal("Your account is restricted. Please contact support.", blocked.Errors[0]);
        }

        // ---------- Siyahı, dayandırma, silmə ----------

        [Fact]
        public async Task The_list_shows_only_my_transfers_active_ones_first()
        {
            var sender = await _app.NewKitAsync(1000m);
            var other = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var first = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 10m, 20)))).Data!;
            var second = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 20m, 3)))).Data!;
            await WithAsync(s => s.CreateAsync(other.User.Id, ToOtherCard(other, recipient, 30m, 3)));
            await WithAsync(s => s.SetActiveAsync(sender.User.Id, second.Id, false));

            var list = (await WithAsync(s => s.GetAsync(sender.User.Id))).ToList();

            Assert.Equal(new[] { first.Id, second.Id }, list.Select(i => i.Id).ToArray());
            Assert.True(list[0].IsActive);
            Assert.False(list[1].IsActive);
        }

        [Fact]
        public async Task Pause_and_resume_work_and_resuming_does_not_send_for_the_paused_months()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 10m, 1)))).Data!;
            await _app.QueryAsync(async db =>
            {
                await db.RecurringTransfers.Where(r => r.Id == item.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastPeriod, "2020-01").SetProperty(r => r.LastError, "old error"));
                return 0;
            });

            var paused = await WithAsync(s => s.SetActiveAsync(sender.User.Id, item.Id, false));
            var resumed = await WithAsync(s => s.SetActiveAsync(sender.User.Id, item.Id, true));

            Assert.False(paused.Data!.IsActive);
            Assert.True(resumed.Data!.IsActive);
            Assert.Null(resumed.Data.LastError);
            Assert.Equal(RecurringTransferRules.PeriodOf(Today()), (await LoadAsync(item.Id)).LastPeriod);       // köhnə aylar yazılmır
        }

        [Fact]
        public async Task Only_the_owner_can_pause_or_delete_a_transfer()
        {
            var sender = await _app.NewKitAsync(1000m);
            var stranger = await _app.NewKitAsync();
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient)))).Data!;

            Assert.True((await WithAsync(s => s.SetActiveAsync(stranger.User.Id, item.Id, false))).IsNotFound);
            Assert.True((await WithAsync(s => s.DeleteAsync(stranger.User.Id, item.Id))).IsNotFound);
            Assert.True((await LoadAsync(item.Id)).IsActive);

            var deleted = await WithAsync(s => s.DeleteAsync(sender.User.Id, item.Id));
            Assert.True(deleted.IsSuccess);
            Assert.Empty((await WithAsync(s => s.GetAsync(sender.User.Id))));
            Assert.True((await WithAsync(s => s.DeleteAsync(sender.User.Id, item.Id))).IsNotFound);
        }

        // ---------- İşin icrası ----------

        [Fact]
        public async Task A_due_transfer_is_sent_once_the_balances_move_and_the_owner_is_told()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(item.Id);

            var sent = await WithAsync(s => s.RunDueAsync());

            Assert.True(sent >= 1);
            Assert.Equal(900m, await _app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(100m, await _app.BalanceAsync(recipient.Standard.Id));
            var stored = await LoadAsync(item.Id);
            Assert.Equal(RecurringTransferRules.PeriodOf(Today()), stored.LastPeriod);
            Assert.Null(stored.LastError);
            var notice = Assert.Single(await _app.NotificationsAsync(sender.User.Id));
            Assert.Equal("Monthly transfer sent", notice.Title);
            Assert.Contains("100.00 AZN was sent to", notice.Message);
            Assert.Contains(await _app.NotificationsAsync(recipient.User.Id), n => n.Title == "Money received");
        }

        [Fact]
        public async Task Running_again_in_the_same_month_does_not_send_again()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(item.Id);

            await WithAsync(s => s.RunDueAsync());
            await WithAsync(s => s.RunDueAsync());
            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(900m, await _app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task Even_if_the_month_marker_is_lost_the_same_month_cannot_pay_twice()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(item.Id);
            await WithAsync(s => s.RunDueAsync());

            await MakeDueAsync(item.Id);                  // işarə itib: eyni ay üçün təkrar vaxtı çatmış görünür
            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(900m, await _app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(100m, await _app.BalanceAsync(recipient.Standard.Id));
        }

        [Fact]
        public async Task A_paused_transfer_is_not_sent()
        {
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await WithAsync(s => s.SetActiveAsync(sender.User.Id, item.Id, false));
            await _app.QueryAsync(async db =>
            {
                await db.RecurringTransfers.Where(r => r.Id == item.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastPeriod, PreviousPeriod()));
                return 0;
            });

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(1000m, await _app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task A_transfer_before_its_day_is_not_sent_yet()
        {
            if (Today().Day >= 28)
            {
                return;       // gün 28 hər gün keçmiş olur: bu hal yalnız ayın əvvəlində yoxlanır
            }
            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 28)))).Data!;
            await MakeDueAsync(item.Id);

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(1000m, await _app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task A_failed_transfer_records_the_reason_tells_the_owner_and_is_not_retried_the_same_day()
        {
            var sender = await _app.NewKitAsync(10m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(item.Id);

            await WithAsync(s => s.RunDueAsync());
            await WithAsync(s => s.RunDueAsync());

            var stored = await LoadAsync(item.Id);
            Assert.Equal("The source card doesn't cover the amount and commission.", stored.LastError);
            Assert.NotEqual(RecurringTransferRules.PeriodOf(Today()), stored.LastPeriod);
            var notice = Assert.Single(await _app.NotificationsAsync(sender.User.Id));
            Assert.Equal("Monthly transfer failed", notice.Title);
            Assert.Contains("We will try again tomorrow.", notice.Message);
            Assert.Equal(10m, await _app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task The_next_day_a_funded_card_makes_the_failed_transfer_go_through()
        {
            var sender = await _app.NewKitAsync(10m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(item.Id);
            await WithAsync(s => s.RunDueAsync());

            // ertəsi gün: cəhd günü dünəndir, müştəri kartı doldurub
            await _app.SetBalanceAsync(sender.Standard.Id, 500m);
            await _app.QueryAsync(async db =>
            {
                await db.RecurringTransfers.Where(r => r.Id == item.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastAttemptDay, "2020-01-01"));
                return 0;
            });
            await WithAsync(s => s.RunDueAsync());

            var stored = await LoadAsync(item.Id);
            Assert.Null(stored.LastError);
            Assert.Equal(RecurringTransferRules.PeriodOf(Today()), stored.LastPeriod);
            Assert.Equal(400m, await _app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(100m, await _app.BalanceAsync(recipient.Standard.Id));
        }

        [Fact]
        public async Task A_monthly_transfer_pays_the_normal_commission_above_the_card_limit()
        {
            var sender = await _app.NewKitAsync(2000m);
            var recipient = await _app.NewKitAsync();
            var item = (await WithAsync(s => s.CreateAsync(sender.User.Id, ToOtherCard(sender, recipient, 600m, 1)))).Data!;
            await MakeDueAsync(item.Id);

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(2000m - 601m, await _app.BalanceAsync(sender.Standard.Id));       // 600 + 1 komissiya
        }

        [Fact]
        public async Task A_failure_of_one_transfer_does_not_stop_the_others()
        {
            var poor = await _app.NewKitAsync(1m);
            var rich = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var failing = (await WithAsync(s => s.CreateAsync(poor.User.Id, ToOtherCard(poor, recipient, 100m, 1)))).Data!;
            var working = (await WithAsync(s => s.CreateAsync(rich.User.Id, ToOtherCard(rich, recipient, 100m, 1)))).Data!;
            await MakeDueAsync(failing.Id);
            await MakeDueAsync(working.Id);

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(900m, await _app.BalanceAsync(rich.Standard.Id));
        }
    }
}
