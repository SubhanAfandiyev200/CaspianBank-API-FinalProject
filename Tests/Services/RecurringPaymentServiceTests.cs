using CaspianBank.Tests.Infrastructure;
using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Bills;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class RecurringPaymentServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public RecurringPaymentServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IRecurringPaymentService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IRecurringPaymentService>());
        }

        private async Task<BillProvider> MobileProviderAsync(string prefixes = "70,77", decimal min = 1m, decimal max = 200m)
        {
            return await _app.QueryAsync(async db =>
            {
                var n = TestData.Next();
                var provider = new BillProvider { Code = "r" + n, Name = "Operator " + n, Category = BillCategory.Mobile, Prefixes = prefixes, IdentifierLength = 10, IdentifierLabel = "Phone number", MinPayment = min, MaxPayment = max };
                db.BillProviders.Add(provider);
                await db.SaveChangesAsync();
                return provider;
            });
        }

        private async Task<string> SubscriberAsync(BillProvider provider, string prefix = "77", decimal balance = 0m)
        {
            var phone = "0" + prefix + (1000000 + TestData.Next() % 9000000).ToString("D7");
            await _app.QueryAsync(async db =>
            {
                db.BillSubscribers.Add(new BillSubscriber { ProviderId = provider.Id, Identifier = phone, FullName = "Elçin Hüseynov", Balance = balance });
                await db.SaveChangesAsync();
                return 0;
            });
            return phone;
        }

        private async Task<RecurringPayment> LoadAsync(int id)
        {
            return await _app.QueryAsync(db => db.RecurringPayments.AsNoTracking().FirstAsync(r => r.Id == id));
        }

        private async Task SetLastPeriodAsync(int id, string period)
        {
            await _app.QueryAsync(async db =>
            {
                await db.RecurringPayments.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastPeriod, period));
                return 0;
            });
        }

        private static string Period()
        {
            return BillingEngine.PeriodOf(LoanEngine.BakuToday(DateTime.UtcNow));
        }

        private async Task<(CustomerKit Kit, BillProvider Provider, string Phone, RecurringPaymentDto Item)> BillSetupAsync(decimal balance = 500m, decimal amount = 20m)
        {
            var kit = await _app.NewKitAsync(balance);
            var provider = await MobileProviderAsync();
            var phone = await SubscriberAsync(provider);
            var created = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = amount, CardId = kit.Standard.Id }));
            Assert.True(created.IsSuccess, string.Join(";", created.Errors));
            return (kit, provider, phone, created.Data!);
        }

        // ---------- Yaratma ----------

        [Fact]
        public async Task Create_saves_a_monthly_bill_payment_without_paying_now()
        {
            var (kit, provider, phone, item) = await BillSetupAsync(500m, 20m);

            Assert.Equal(provider.Id, item.ProviderId);
            Assert.Equal(provider.Name, item.ProviderName);
            Assert.Equal("Mobile", item.Category);
            Assert.Equal($"{phone[..3]} {phone.Substring(3, 3)} {phone.Substring(6, 2)} {phone.Substring(8, 2)}", item.Identifier);
            Assert.Equal(20m, item.Amount);
            Assert.Equal("Active", item.Status);
            Assert.True(item.IsActive);
            Assert.Equal("•••• " + kit.Standard.CardNumber[^4..], item.CardLabel);
            Assert.Equal(500m, await _app.BalanceAsync(kit.Standard.Id));
            var stored = await LoadAsync(item.Id);
            Assert.Equal(Period(), stored.LastPeriod);                // ilk ödəniş bu ay üçün sayılır, növbəti ayın 1-ində
            Assert.Equal(phone, stored.Identifier);
        }

        [Fact]
        public async Task The_next_run_of_a_new_bill_payment_is_the_first_of_next_month()
        {
            var (_, _, _, item) = await BillSetupAsync();
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            Assert.Equal(new DateTime(today.Year, today.Month, 1).AddMonths(1), item.NextRunDate);
        }

        [Fact]
        public async Task Create_checks_the_number_the_operator_the_amount_and_the_card()
        {
            var kit = await _app.NewKitAsync(500m);
            var stranger = await _app.NewKitAsync(500m);
            var blocked = await _app.NewCardAsync(kit.User.Id, CardTier.Silver, 500m, blocked: true);
            var provider = await MobileProviderAsync("70,77", 5m, 50m);
            var phone = await SubscriberAsync(provider);

            var badNumber = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = "123", Amount = 10m, CardId = kit.Standard.Id }));
            var low = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 4m, CardId = kit.Standard.Id }));
            var high = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 51m, CardId = kit.Standard.Id }));
            var foreignCard = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 10m, CardId = stranger.Standard.Id }));
            var blockedCard = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 10m, CardId = blocked.Id }));
            var noProvider = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = 999999, Identifier = phone, Amount = 10m, CardId = kit.Standard.Id }));

            Assert.Equal("Enter the phone number like 055 123 45 67.", badNumber.Errors[0]);
            Assert.Equal("The amount must be between 5.00 and 50.00 AZN.", low.Errors[0]);
            Assert.Equal("The amount must be between 5.00 and 50.00 AZN.", high.Errors[0]);
            Assert.Equal("Choose one of your own active cards.", foreignCard.Errors[0]);
            Assert.Equal("Choose one of your own active cards.", blockedCard.Errors[0]);
            Assert.Equal("This provider isn't available right now.", noProvider.Errors[0]);
        }

        [Fact]
        public async Task Choosing_the_same_payment_again_updates_it_instead_of_adding_another()
        {
            var (kit, provider, phone, item) = await BillSetupAsync(500m, 20m);
            var silver = await _app.NewCardAsync(kit.User.Id, CardTier.Silver, 100m);
            await WithAsync(s => s.SetActiveAsync(kit.User.Id, item.Id, false));

            var again = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 35m, CardId = silver.Id }));

            Assert.True(again.IsSuccess);
            Assert.Equal(item.Id, again.Data!.Id);
            Assert.Equal(35m, again.Data.Amount);
            Assert.True(again.Data.IsActive);                         // dayandırılmışdı, yenidən başladı
            Assert.Single(await WithAsync(s => s.GetAllAsync(kit.User.Id)));
        }

        [Fact]
        public async Task Create_is_limited_to_ten_payments_and_refused_for_restricted_accounts()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await MobileProviderAsync();
            for (var i = 0; i < 10; i++)
            {
                var phone = await SubscriberAsync(provider);
                Assert.True((await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = phone, Amount = 5m, CardId = kit.Standard.Id }))).IsSuccess);
            }

            var extra = await SubscriberAsync(provider);
            var eleventh = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = extra, Amount = 5m, CardId = kit.Standard.Id }));
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var restricted = await WithAsync(s => s.CreateAsync(kit.User.Id, new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = extra, Amount = 5m, CardId = kit.Standard.Id }));
            var ghost = await WithAsync(s => s.CreateAsync("nobody", new CreateRecurringPaymentDto { ProviderId = provider.Id, Identifier = extra, Amount = 5m, CardId = kit.Standard.Id }));

            Assert.Equal("You can have up to 10 monthly payments. Remove one to add another.", eleventh.Errors[0]);
            Assert.Equal("Your account is restricted. Please contact support.", restricted.Errors[0]);
            Assert.True(ghost.IsNotFound);
        }

        // ---------- Kredit ----------

        private async Task<(CustomerKit Borrower, LoanSummaryDtoAlias Loan)> ActiveLoanAsync()
        {
            var borrower = await _app.NewBorrowerAsync(3000m, 5000m);
            var loan = await _app.ActiveLoanAsync(borrower, 600m, 6);
            return (borrower, new LoanSummaryDtoAlias(loan.Id, loan.ContractNumber));
        }

        private sealed record LoanSummaryDtoAlias(int Id, string ContractNumber);

        [Fact]
        public async Task A_loan_can_be_added_by_code_and_needs_no_amount()
        {
            var (borrower, loan) = await ActiveLoanAsync();

            var result = await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = loan.ContractNumber.ToLowerInvariant(), Amount = 0m, CardId = borrower.Standard.Id }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Null(result.Data!.ProviderId);
            Assert.Equal("Caspian loan", result.Data.ProviderName);
            Assert.Equal("Loan", result.Data.Category);
            Assert.Equal(loan.ContractNumber, result.Data.Identifier);
            Assert.Equal(0m, result.Data.Amount);
            var stored = await LoadAsync(result.Data.Id);
            Assert.Equal(RecurringRules.StartingPeriod(true, LoanEngine.BakuToday(DateTime.UtcNow)), stored.LastPeriod);
        }

        [Fact]
        public async Task A_loan_code_that_is_not_active_is_refused()
        {
            var borrower = await _app.NewBorrowerAsync(3000m);
            var pending = await _app.ApplyAsync(borrower);

            var closed = await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = pending.ContractNumber, CardId = borrower.Standard.Id }));
            var unknown = await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = "KR-2026-999999", CardId = borrower.Standard.Id }));
            var badFormat = await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = "123", CardId = borrower.Standard.Id }));

            Assert.Equal("We couldn't find an active loan with this code.", closed.Errors[0]);
            Assert.Equal("We couldn't find an active loan with this code.", unknown.Errors[0]);
            Assert.Equal("Enter the loan code like KR-2026-000123.", badFormat.Errors[0]);
        }

        // ---------- Siyahı, dayandırma, silmə ----------

        [Fact]
        public async Task The_list_shows_the_status_and_the_owner_only_sees_their_own()
        {
            var (kit, _, _, item) = await BillSetupAsync();
            var other = await _app.NewKitAsync();

            await WithAsync(s => s.SetActiveAsync(kit.User.Id, item.Id, false));
            var mine = (await WithAsync(s => s.GetAllAsync(kit.User.Id))).ToList();
            var theirs = (await WithAsync(s => s.GetAllAsync(other.User.Id))).ToList();

            Assert.Equal("Paused", Assert.Single(mine).Status);
            Assert.Empty(theirs);
        }

        [Fact]
        public async Task A_failed_payment_shows_the_failed_status_with_the_reason()
        {
            var (kit, _, _, item) = await BillSetupAsync(5m, 50m);
            await SetLastPeriodAsync(item.Id, "2020-01");

            await WithAsync(s => s.RunDueAsync());

            var listed = Assert.Single(await WithAsync(s => s.GetAllAsync(kit.User.Id)));
            Assert.Equal("Failed", listed.Status);
            Assert.Equal("That card doesn't have enough balance for this payment.", listed.LastError);
        }

        [Fact]
        public async Task Only_the_owner_can_pause_resume_or_delete()
        {
            var (kit, _, _, item) = await BillSetupAsync();
            var stranger = await _app.NewKitAsync();

            Assert.True((await WithAsync(s => s.SetActiveAsync(stranger.User.Id, item.Id, false))).IsNotFound);
            Assert.True((await WithAsync(s => s.DeleteAsync(stranger.User.Id, item.Id))).IsNotFound);
            Assert.True((await WithAsync(s => s.DeleteAsync(kit.User.Id, item.Id))).IsSuccess);
            Assert.True((await WithAsync(s => s.DeleteAsync(kit.User.Id, item.Id))).IsNotFound);
        }

        [Fact]
        public async Task Resuming_clears_the_old_error()
        {
            var (kit, _, _, item) = await BillSetupAsync(5m, 50m);
            await SetLastPeriodAsync(item.Id, "2020-01");
            await WithAsync(s => s.RunDueAsync());
            Assert.NotNull((await LoadAsync(item.Id)).LastError);

            var resumed = await WithAsync(s => s.SetActiveAsync(kit.User.Id, item.Id, true));

            Assert.Null(resumed.Data!.LastError);
            Assert.Equal("Active", resumed.Data.Status);
            Assert.Null((await LoadAsync(item.Id)).LastAttemptDay);
        }

        // ---------- İcra ----------

        [Fact]
        public async Task A_due_bill_payment_is_made_recorded_and_the_owner_is_told()
        {
            var (kit, provider, phone, item) = await BillSetupAsync(500m, 20m);
            await SetLastPeriodAsync(item.Id, "2020-01");

            var paid = await WithAsync(s => s.RunDueAsync());

            Assert.True(paid >= 1);
            Assert.Equal(480m, await _app.BalanceAsync(kit.Standard.Id));
            var stored = await LoadAsync(item.Id);
            Assert.Equal(Period(), stored.LastPeriod);
            Assert.Null(stored.LastError);
            var payment = await _app.QueryAsync(db => db.BillPayments.AsNoTracking().SingleAsync(p => p.UserId == kit.User.Id));
            Assert.Equal(20m, payment.Amount);
            Assert.Equal(phone, payment.Identifier);
            var notice = Assert.Single(await _app.NotificationsAsync(kit.User.Id), n => n.Category == NotificationCategory.Bill);
            Assert.Equal("Monthly payment made", notice.Title);
            Assert.Contains("20.00 AZN was paid to " + provider.Name, notice.Message);
        }

        [Fact]
        public async Task Running_again_or_losing_the_marker_does_not_pay_a_month_twice()
        {
            var (kit, _, _, item) = await BillSetupAsync(500m, 20m);
            await SetLastPeriodAsync(item.Id, "2020-01");
            await WithAsync(s => s.RunDueAsync());
            await WithAsync(s => s.RunDueAsync());

            await SetLastPeriodAsync(item.Id, "2020-01");        // işarə itib
            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(480m, await _app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(1, await _app.QueryAsync(db => db.BillPayments.CountAsync(p => p.UserId == kit.User.Id)));
        }

        [Fact]
        public async Task A_paused_payment_is_not_made()
        {
            var (kit, _, _, item) = await BillSetupAsync(500m, 20m);
            await WithAsync(s => s.SetActiveAsync(kit.User.Id, item.Id, false));
            await SetLastPeriodAsync(item.Id, "2020-01");

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(500m, await _app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task A_payment_already_done_this_month_is_not_repeated()
        {
            var (kit, _, _, item) = await BillSetupAsync(500m, 20m);     // yaradılanda bu ay işlənmiş sayılır

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(500m, await _app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task A_failed_payment_is_recorded_announced_once_and_retried_the_next_day()
        {
            var (kit, _, _, item) = await BillSetupAsync(5m, 50m);
            await SetLastPeriodAsync(item.Id, "2020-01");

            await WithAsync(s => s.RunDueAsync());
            await WithAsync(s => s.RunDueAsync());                // eyni gün təkrar cəhd yoxdur

            var failed = await LoadAsync(item.Id);
            Assert.Equal("That card doesn't have enough balance for this payment.", failed.LastError);
            Assert.NotEqual(Period(), failed.LastPeriod);
            var notice = Assert.Single(await _app.NotificationsAsync(kit.User.Id), n => n.Category == NotificationCategory.Bill);
            Assert.Equal("Monthly payment failed", notice.Title);
            Assert.Contains("We will try again tomorrow.", notice.Message);

            // ertəsi gün kart doldurulub
            await _app.SetBalanceAsync(kit.Standard.Id, 500m);
            await _app.QueryAsync(async db =>
            {
                await db.RecurringPayments.Where(r => r.Id == item.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.LastAttemptDay, "2020-01-01"));
                return 0;
            });
            await WithAsync(s => s.RunDueAsync());

            var fixedItem = await LoadAsync(item.Id);
            Assert.Null(fixedItem.LastError);
            Assert.Equal(Period(), fixedItem.LastPeriod);
            Assert.Equal(450m, await _app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task A_due_loan_installment_is_paid_by_the_monthly_payment()
        {
            var (borrower, loan) = await ActiveLoanAsync();
            var created = (await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = loan.ContractNumber, CardId = borrower.Standard.Id }))).Data!;
            await SetLastPeriodAsync(created.Id, "2020-01");
            var before = await _app.BalanceAsync(borrower.Standard.Id);

            await WithAsync(s => s.RunDueAsync());

            var after = await _app.BalanceAsync(borrower.Standard.Id);
            Assert.True(after < before);
            var stored = await LoadAsync(created.Id);
            Assert.Equal(Period(), stored.LastPeriod);
            Assert.Null(stored.LastError);
            var notice = Assert.Single(await _app.NotificationsAsync(borrower.User.Id), n => n.Title == "Monthly payment made");
            Assert.Contains("Caspian loan", notice.Message);
            Assert.Equal(1, (await _app.GetLoanAsync(loan.Id)).PaymentRecords.Count);
        }

        [Fact]
        public async Task A_loan_with_nothing_due_is_marked_done_quietly()
        {
            var (borrower, loan) = await ActiveLoanAsync();
            var created = (await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = loan.ContractNumber, CardId = borrower.Standard.Id }))).Data!;
            await SetLastPeriodAsync(created.Id, "2020-01");
            // bütün aylıq ödənişlər artıq ödənilib: "CanPay" yalan olur
            await _app.QueryAsync(async db =>
            {
                var installments = await db.LoanInstallments.Where(i => i.LoanId == loan.Id).ToListAsync();
                foreach (var installment in installments)
                {
                    installment.PaidAmount = installment.PrincipalAmount + installment.InterestAmount;
                }
                await db.SaveChangesAsync();
                return 0;
            });
            var before = await _app.BalanceAsync(borrower.Standard.Id);

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(before, await _app.BalanceAsync(borrower.Standard.Id));
            Assert.Equal(Period(), (await LoadAsync(created.Id)).LastPeriod);
        }

        [Fact]
        public async Task A_closed_loan_stops_its_monthly_payment_and_tells_the_owner()
        {
            var (borrower, loan) = await ActiveLoanAsync();
            var created = (await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = loan.ContractNumber, CardId = borrower.Standard.Id }))).Data!;
            await SetLastPeriodAsync(created.Id, "2020-01");
            await _app.QueryAsync(async db =>
            {
                await db.Loans.Where(l => l.Id == loan.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LoanStatus.Closed));
                return 0;
            });

            await WithAsync(s => s.RunDueAsync());

            var stored = await LoadAsync(created.Id);
            Assert.False(stored.IsActive);
            Assert.Equal("The loan is closed.", stored.LastError);
            var notice = Assert.Single(await _app.NotificationsAsync(borrower.User.Id), n => n.Title == "Monthly loan payment stopped");
            Assert.Contains(loan.ContractNumber, notice.Message);
        }

        [Fact]
        public async Task A_loan_is_not_paid_in_the_middle_of_the_month()
        {
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            if (today.Day == DateTime.DaysInMonth(today.Year, today.Month))
            {
                return;        // ayın son günündə kredit ödənişi vaxtıdır
            }
            var (borrower, loan) = await ActiveLoanAsync();
            var created = (await WithAsync(s => s.CreateAsync(borrower.User.Id, new CreateRecurringPaymentDto { ProviderId = null, Identifier = loan.ContractNumber, CardId = borrower.Standard.Id }))).Data!;
            var previous = RecurringRules.StartingPeriod(true, today);
            await SetLastPeriodAsync(created.Id, previous);
            var before = await _app.BalanceAsync(borrower.Standard.Id);

            await WithAsync(s => s.RunDueAsync());

            Assert.Equal(before, await _app.BalanceAsync(borrower.Standard.Id));
        }
    }
}
