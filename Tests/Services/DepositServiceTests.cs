using CaspianBank.Tests.Infrastructure;
using Domain.Deposits;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Deposits;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class DepositServiceTests
    {
        private static int _term = 1000;
        private static Task<DepositPlan>? _plan6;
        private static Task<DepositPlan>? _plan12;

        private static string NewRequest()
        {
            return Guid.NewGuid().ToString();
        }

        private static async Task<T> WithAsync<T>(TestApp app, Func<IDepositService, Task<T>> action)
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IDepositService>());
        }

        // Hər test öz planını yaradır: seed planlarına bağlı qalmır
        private static async Task<DepositPlan> NewPlanAsync(TestApp app, int? term = null, decimal rate = 12m, decimal min = 100m, decimal max = 100000m, bool active = true)
        {
            // Bir müddət üçün yalnız bir plan ola bilər (unikal indeks): ortaq bazada hər test öz müddətini götürür. Ayrıca bazada 6, 12 kimi real müddətlər işlənir
            var months = term ?? Interlocked.Increment(ref _term);
            return await app.QueryAsync(async db =>
            {
                var plan = new DepositPlan { TermMonths = months, AnnualRate = rate, MinAmount = min, MaxAmount = max, IsActive = active };
                db.DepositPlans.Add(plan);
                await db.SaveChangesAsync();
                return plan;
            });
        }

        private static OpenDepositDto Open(DepositPlan plan, CustomerKit kit, decimal amount, int? payout = null, int? source = null, string? request = null)
        {
            return new OpenDepositDto
            {
                PlanId = plan.Id,
                Amount = amount,
                SourceCardId = source ?? kit.Standard.Id,
                PayoutCardId = payout ?? kit.Standard.Id,
                RequestId = request ?? NewRequest()
            };
        }

        // Ortaq bazada 6 aylıq (12%) və 12 aylıq (10%) planlar bir dəfə yaranır, onları dəyişən test yoxdur
        private static Task<DepositPlan> Plan6Async(TestApp app)
        {
            return _plan6 ??= NewPlanAsync(app, 6, 12m);
        }

        private static Task<DepositPlan> Plan12Async(TestApp app)
        {
            return _plan12 ??= NewPlanAsync(app, 12, 10m);
        }

        // Depoziti "keçmişə çəkir": müddət bitib (vaxt DB-də dəyişir)
        private static async Task AgeAsync(TestApp app, string reference, int daysPastMaturity = 1)
        {
            await app.QueryAsync(async db =>
            {
                var deposit = await db.Deposits.FirstAsync(d => d.Reference == reference);
                deposit.MaturesAt = DateTime.UtcNow.AddDays(-daysPastMaturity);
                deposit.StartedAt = deposit.MaturesAt.AddMonths(-deposit.TermMonths);
                await db.SaveChangesAsync();
                return 0;
            });
        }

        // ---------- Açılış ----------

        [Fact]
        public async Task Opening_a_deposit_locks_the_money_and_calculates_the_interest()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(2000m);
            var plan = await Plan6Async(app);

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var deposit = result.Data!;
            Assert.Equal(1000m, deposit.Principal);
            Assert.Equal(60m, deposit.ExpectedInterest);                 // 1000 x 12% x 6/12
            Assert.Equal(1060m, deposit.TotalAtMaturity);
            Assert.Equal(6, deposit.TermMonths);
            Assert.Equal(12m, deposit.AnnualRate);
            Assert.Equal("Active", deposit.Status);
            Assert.Equal(0m, deposit.AccruedInterest);
            Assert.Equal(0, deposit.ProgressPercent);
            Assert.InRange(deposit.DaysLeft, 180, 186);
            Assert.StartsWith("DP-", deposit.Reference);
            Assert.Equal(1000m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Opening_a_deposit_tells_the_money_staff()
        {
            var app = await TestApp.SharedAsync();
            var accountant = await app.NewStaffAsync(Domain.Constants.Roles.Accountant);
            var support = await app.NewStaffAsync(Domain.Constants.Roles.CustomerSupport);
            var kit = await app.NewKitAsync(2000m);
            var plan = await Plan6Async(app);

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 700m)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var note = Assert.Single(await app.NotificationsAsync(accountant.Id), n => n.Title == "New term deposit" && n.Message.Contains(kit.User.Name));
            Assert.Equal("/Admin/Deposits", note.LinkUrl);
            Assert.DoesNotContain(await app.NotificationsAsync(support.Id), n => n.Title == "New term deposit" && n.Message.Contains(kit.User.Name));
        }

        [Fact]
        public async Task Opening_writes_the_journal_row_the_notification_and_the_treasury_entry()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(2000m);
            var plan = await Plan12Async(app);

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 500m)));

            var row = Assert.Single(await app.TransactionsAsync(kit.Standard.Id));
            Assert.Equal(TransactionType.DepositOpen, row.Type);
            Assert.False(row.IsIncome);
            Assert.Equal(500m, row.Amount);
            Assert.Equal(1500m, row.BalanceAfter);
            Assert.Equal("Term deposit · 12 months", row.Description);
            Assert.Equal(result.Data!.Reference, row.Reference);

            var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id));
            Assert.Equal("Deposit opened", notice.Title);
            Assert.Equal(NotificationCategory.Deposit, notice.Category);
            Assert.Contains("500.00 AZN is locked for 12 months at 10% a year", notice.Message);
            Assert.Contains("You will get 550.00 AZN", notice.Message);

            var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == result.Data.Reference).ToListAsync());
            var entry = Assert.Single(entries);
            Assert.Equal(TreasuryEntryKind.DepositReceived, entry.Kind);
            Assert.Equal(500m, entry.Amount);
        }

        [Fact]
        public async Task The_plan_rate_at_opening_time_is_fixed_for_the_deposit()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(2000m);
            var plan = await NewPlanAsync(app, rate: 12m);
            var opened = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));

            await WithAsync(app, s => s.UpdatePlanAsync(plan.Id, new UpdateDepositPlanDto { AnnualRate = 5m, MinAmount = 100m, MaxAmount = 100000m, IsActive = true }));

            var overview = await WithAsync(app, s => s.GetOverviewAsync(kit.User.Id));
            var deposit = Assert.Single(overview.Deposits, d => d.Reference == opened.Data!.Reference);
            Assert.Equal(12m, deposit.AnnualRate);
            Assert.Equal(DepositRules.Interest(1000m, 12m, plan.TermMonths), deposit.ExpectedInterest);
        }

        [Theory]
        [InlineData(99.99, false)]
        [InlineData(100, true)]
        [InlineData(5000, true)]
        [InlineData(5000.01, false)]
        public async Task The_amount_must_be_inside_the_plan_limits(double amount, bool allowed)
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(10000m);
            var plan = await NewPlanAsync(app, rate: 12m, min: 100m, max: 5000m);

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, (decimal)amount)));

            Assert.Equal(allowed, result.IsSuccess);
            if (!allowed)
            {
                Assert.Equal("The amount is outside the limits of this deposit term.", result.Errors[0]);
                Assert.Equal(10000m, await app.BalanceAsync(kit.Standard.Id));
            }
        }

        [Fact]
        public async Task An_inactive_or_unknown_plan_is_not_available()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var inactive = await NewPlanAsync(app, active: false);

            var off = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(inactive, kit, 200m)));
            var unknown = await WithAsync(app, s => s.OpenAsync(kit.User.Id, new OpenDepositDto { PlanId = 999999, Amount = 200m, SourceCardId = kit.Standard.Id, PayoutCardId = kit.Standard.Id, RequestId = NewRequest() }));

            Assert.Equal("This deposit term isn't available right now.", off.Errors[0]);
            Assert.Equal("This deposit term isn't available right now.", unknown.Errors[0]);
        }

        [Fact]
        public async Task Not_enough_money_on_the_card_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(150m);
            var plan = await NewPlanAsync(app);

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m)));

            Assert.Equal("There is not enough money on this card.", result.Errors[0]);
            Assert.Equal(150m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_source_card_must_be_my_own_and_active()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var stranger = await app.NewKitAsync(1000m);
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 1000m, blocked: true);
            var plan = await NewPlanAsync(app);

            var foreign = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, source: stranger.Standard.Id)));
            var blockedResult = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, source: blocked.Id)));

            Assert.Equal("We couldn't find the card you chose to pay from.", foreign.Errors[0]);
            Assert.Equal("The card you chose to pay from is blocked.", blockedResult.Errors[0]);
            Assert.Equal(1000m, await app.BalanceAsync(stranger.Standard.Id));
        }

        [Fact]
        public async Task The_money_must_come_back_to_an_own_active_non_cashback_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var stranger = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m, blocked: true);
            var plan = await NewPlanAsync(app);

            var cashback = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, payout: kit.Cashback.Id)));
            var foreign = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, payout: stranger.Standard.Id)));
            var blockedResult = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, payout: blocked.Id)));

            var message = "The money must come back to one of your active cards (not the Cashback card).";
            Assert.Equal(message, cashback.Errors[0]);
            Assert.Equal(message, foreign.Errors[0]);
            Assert.Equal(message, blockedResult.Errors[0]);
            Assert.Equal(1000m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task A_restricted_customer_cannot_open_a_deposit_and_an_unknown_one_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var plan = await NewPlanAsync(app);
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });

            var restricted = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m)));
            var unknown = await WithAsync(app, s => s.OpenAsync("nobody", Open(plan, kit, 200m)));

            Assert.Equal("Your account is restricted. Please contact support.", restricted.Errors[0]);
            Assert.True(unknown.IsNotFound);
        }

        [Fact]
        public async Task Opening_validates_the_request()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var plan = await NewPlanAsync(app);
            var model = Open(plan, kit, 200m);
            model.RequestId = "bad";

            var result = await WithAsync(app, s => s.OpenAsync(kit.User.Id, model));

            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid request id.", result.Errors);
        }

        [Fact]
        public async Task The_same_request_id_opens_only_one_deposit()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var plan = await NewPlanAsync(app);
            var request = NewRequest();

            var first = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, request: request)));
            var again = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, request: request)));

            Assert.True(first.IsSuccess);
            Assert.True(again.IsSuccess);
            Assert.Equal(first.Data!.Reference, again.Data!.Reference);
            Assert.Equal(800m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(1, await app.QueryAsync(db => db.Deposits.CountAsync(d => d.UserId == kit.User.Id)));
        }

        [Fact]
        public async Task Parallel_requests_with_one_id_open_one_deposit()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var plan = await NewPlanAsync(app);
            var request = NewRequest();

            var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m, request: request)))));

            Assert.All(results, r => Assert.True(r.IsSuccess, string.Join(";", r.Errors)));
            Assert.Equal(800m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(1, await app.QueryAsync(db => db.Deposits.CountAsync(d => d.UserId == kit.User.Id)));
        }

        [Fact]
        public async Task Parallel_deposits_never_spend_more_than_the_balance()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(500m);
            var plan = await NewPlanAsync(app);

            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m)))));

            Assert.Equal(2, results.Count(r => r.IsSuccess));
            Assert.Equal(100m, await app.BalanceAsync(kit.Standard.Id));
        }

        // ---------- Siyahı ----------

        [Fact]
        public async Task Overview_lists_active_plans_my_deposits_and_the_totals()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(5000m);
            var plan = await Plan6Async(app);
            var inactive = await NewPlanAsync(app, rate: 20m, active: false);
            await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));
            await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 500m)));

            var overview = await WithAsync(app, s => s.GetOverviewAsync(kit.User.Id));

            Assert.Contains(overview.Plans, p => p.Id == plan.Id);
            Assert.DoesNotContain(overview.Plans, p => p.Id == inactive.Id);
            Assert.Equal(2, overview.Deposits.Count);
            Assert.Equal(1500m, overview.ActivePrincipal);
            Assert.Equal(90m, overview.ActiveExpectedInterest);
            Assert.Equal(0m, overview.InterestEarned);
        }

        [Fact]
        public async Task Overview_of_another_customer_is_empty()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(5000m);
            var other = await app.NewKitAsync();
            var plan = await NewPlanAsync(app);
            await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));

            var overview = await WithAsync(app, s => s.GetOverviewAsync(other.User.Id));

            Assert.Empty(overview.Deposits);
            Assert.Equal(0m, overview.ActivePrincipal);
        }

        // ---------- Admin ----------

        [Fact]
        public async Task Admin_page_filters_by_status_and_search_and_shows_totals()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(5000m, "Zaur", "Deposito");
            var plan = await NewPlanAsync(app);
            var opened = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));

            var found = await WithAsync(app, s => s.GetPageAsync(new DepositFilterDto { Search = "Deposito", Status = "Active" }));
            var notPaid = await WithAsync(app, s => s.GetPageAsync(new DepositFilterDto { Search = "Deposito", Status = "Paid" }));
            var byReference = await WithAsync(app, s => s.GetPageAsync(new DepositFilterDto { Search = opened.Data!.Reference }));

            Assert.True(found.IsSuccess);
            var item = Assert.Single(found.Data!.Items);
            Assert.Equal("Zaur Deposito", item.CustomerName);
            Assert.Equal(1000m, item.Principal);
            Assert.True(found.Data.ActiveCount >= 1);
            Assert.True(found.Data.ActivePrincipal >= 1000m);
            Assert.Empty(notPaid.Data!.Items);
            Assert.Single(byReference.Data!.Items);
        }

        [Fact]
        public async Task Admin_page_rejects_an_unknown_status_and_clamps_the_paging()
        {
            var app = await TestApp.SharedAsync();

            var bad = await WithAsync(app, s => s.GetPageAsync(new DepositFilterDto { Status = "Gone" }));
            var clamped = await WithAsync(app, s => s.GetPageAsync(new DepositFilterDto { Page = -3, PageSize = 100000 }));

            Assert.Equal("Unknown deposit status.", bad.Errors[0]);
            Assert.True(clamped.IsSuccess);
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(100, clamped.Data.PageSize);
        }

        [Fact]
        public async Task Admin_can_change_a_plan_and_switch_it_off()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var plan = await NewPlanAsync(app);

            var updated = await WithAsync(app, s => s.UpdatePlanAsync(plan.Id, new UpdateDepositPlanDto { AnnualRate = 9.5m, MinAmount = 50m, MaxAmount = 2000m, IsActive = false }));
            var open = await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 200m)));

            Assert.True(updated.IsSuccess);
            Assert.Equal(9.5m, updated.Data!.AnnualRate);
            Assert.Equal(50m, updated.Data.MinAmount);
            Assert.False(updated.Data.IsActive);
            Assert.False(open.IsSuccess);
        }

        [Fact]
        public async Task Plan_update_validates_and_reports_unknown_plans()
        {
            var app = await TestApp.SharedAsync();
            var plan = await NewPlanAsync(app);

            var invalid = await WithAsync(app, s => s.UpdatePlanAsync(plan.Id, new UpdateDepositPlanDto { AnnualRate = 50m, MinAmount = 10m, MaxAmount = 100m }));
            var unknown = await WithAsync(app, s => s.UpdatePlanAsync(999999, new UpdateDepositPlanDto { AnnualRate = 5m, MinAmount = 10m, MaxAmount = 100m }));

            Assert.False(invalid.IsSuccess);
            Assert.True(unknown.IsNotFound);
        }

        // ---------- Müddətin bitməsi və aylıq faiz ----------

        [Fact]
        public async Task A_matured_deposit_returns_the_principal_and_the_interest_to_the_card()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;
                await AgeAsync(app, opened.Reference);

                var paid = await WithAsync(app, s => s.RunMaturityAsync());

                Assert.Equal(1, paid);
                Assert.Equal(2060m, await app.BalanceAsync(kit.Standard.Id));      // 1000 + 1000 qaldı + 60 faiz
                var rows = await app.TransactionsAsync(kit.Standard.Id);
                var payout = Assert.Single(rows, r => r.Type == TransactionType.DepositPayout);
                var interest = Assert.Single(rows, r => r.Type == TransactionType.DepositInterest);
                Assert.Equal(1000m, payout.Amount);
                Assert.Equal(60m, interest.Amount);
                Assert.Equal("DM-" + opened.Reference.Substring(3), payout.Reference);
                Assert.Equal(payout.Reference, interest.Reference);
                Assert.Equal("Deposit returned · 6 months", payout.Description);

                var deposit = await app.QueryAsync(db => db.Deposits.AsNoTracking().FirstAsync(d => d.Reference == opened.Reference));
                Assert.Equal(DepositStatus.Paid, deposit.Status);
                Assert.NotNull(deposit.PaidAt);
                Assert.Equal(kit.Standard.Id, deposit.PaidToCardId);

                var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Deposit matured");
                Assert.Contains("1,060.00 AZN (1,000.00 AZN + 60.00 AZN interest)", notice.Message);
                Assert.Contains("•••• " + kit.Standard.CardNumber[^4..], notice.Message);
            }
        }

        [Fact]
        public async Task The_treasury_records_the_returned_deposit_and_the_interest_paid()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;
                await AgeAsync(app, opened.Reference);

                await WithAsync(app, s => s.RunMaturityAsync());

                var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == "DM-" + opened.Reference.Substring(3)).ToListAsync());
                Assert.Equal(-1000m, entries.Single(e => e.Kind == TreasuryEntryKind.DepositReturned).Amount);
                Assert.Equal(-60m, entries.Single(e => e.Kind == TreasuryEntryKind.DepositInterest).Amount);
            }
        }

        [Fact]
        public async Task Maturity_runs_pay_once_even_when_repeated_or_run_in_parallel()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;
                await AgeAsync(app, opened.Reference);

                var runs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => WithAsync(app, s => s.RunMaturityAsync())));
                var later = await WithAsync(app, s => s.RunMaturityAsync());

                Assert.Equal(1, runs.Sum());
                Assert.Equal(0, later);
                Assert.Equal(2060m, await app.BalanceAsync(kit.Standard.Id));
                Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Deposit matured");
            }
        }

        [Fact]
        public async Task A_deposit_that_has_not_matured_is_not_paid()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)));

                var paid = await WithAsync(app, s => s.RunMaturityAsync());

                Assert.Equal(0, paid);
                Assert.Equal(1000m, await app.BalanceAsync(kit.Standard.Id));
            }
        }

        [Fact]
        public async Task A_blocked_payout_card_sends_the_money_to_another_active_card()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m, payout: silver.Id)))).Data!;
                await AgeAsync(app, opened.Reference);
                await app.QueryAsync(async db =>
                {
                    await db.Cards.Where(c => c.Id == silver.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlocked, true));
                    return 0;
                });

                await WithAsync(app, s => s.RunMaturityAsync());

                Assert.Equal(0m, await app.BalanceAsync(silver.Id));
                Assert.Equal(2060m, await app.BalanceAsync(kit.Standard.Id));        // Standard kart seçilir (Cashback yox)
            }
        }

        [Fact]
        public async Task A_deposit_waits_when_the_customer_has_no_usable_card()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;
                await AgeAsync(app, opened.Reference);
                await app.QueryAsync(async db =>
                {
                    await db.Cards.Where(c => c.Id == kit.Standard.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlocked, true));
                    return 0;
                });

                var paid = await WithAsync(app, s => s.RunMaturityAsync());

                Assert.Equal(0, paid);
                Assert.Equal(DepositStatus.Active, (await app.QueryAsync(db => db.Deposits.AsNoTracking().FirstAsync(d => d.Reference == opened.Reference))).Status);

                await app.QueryAsync(async db =>
                {
                    await db.Cards.Where(c => c.Id == kit.Standard.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlocked, false));
                    return 0;
                });
                Assert.Equal(1, await WithAsync(app, s => s.RunMaturityAsync()));
            }
        }

        [Fact]
        public async Task Monthly_interest_is_posted_for_each_elapsed_month_and_adds_up_at_maturity()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;

                // 2 ay və 3 gün keçib
                await app.QueryAsync(async db =>
                {
                    var deposit = await db.Deposits.FirstAsync(d => d.Reference == opened.Reference);
                    deposit.StartedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-3);
                    deposit.MaturesAt = deposit.StartedAt.AddMonths(6);
                    await db.SaveChangesAsync();
                    return 0;
                });
                await WithAsync(app, s => s.RunMaturityAsync());

                var middle = (await WithAsync(app, s => s.GetOverviewAsync(kit.User.Id))).Deposits.Single();
                Assert.Equal(2, middle.Postings.Count);
                Assert.Equal(new[] { 1, 2 }, middle.Postings.Select(p => p.Number).ToArray());
                Assert.Equal(20m, middle.AccruedInterest);                  // 60 / 6 x 2
                Assert.Equal(1000m, await app.BalanceAsync(kit.Standard.Id)); // pul hələ qayıtmayıb

                await AgeAsync(app, opened.Reference);
                await WithAsync(app, s => s.RunMaturityAsync());
                var postings = await app.QueryAsync(db => db.DepositInterestPostings.AsNoTracking().Where(p => p.Deposit.Reference == opened.Reference).ToListAsync());
                Assert.Equal(6, postings.Count);
                Assert.Equal(60m, postings.Sum(p => p.Amount));
            }
        }

        [Fact]
        public async Task A_paid_deposit_shows_the_full_interest_and_counts_in_interest_earned()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(2000m);
                var plan = await NewPlanAsync(app, 6, 12m);
                var opened = (await WithAsync(app, s => s.OpenAsync(kit.User.Id, Open(plan, kit, 1000m)))).Data!;
                await AgeAsync(app, opened.Reference);
                await WithAsync(app, s => s.RunMaturityAsync());

                var overview = await WithAsync(app, s => s.GetOverviewAsync(kit.User.Id));

                var deposit = Assert.Single(overview.Deposits);
                Assert.Equal("Paid", deposit.Status);
                Assert.Equal(60m, deposit.AccruedInterest);
                Assert.Equal(100, deposit.ProgressPercent);
                Assert.Equal(0, deposit.DaysLeft);
                Assert.Equal(60m, overview.InterestEarned);
                Assert.Equal(0m, overview.ActivePrincipal);
            }
        }
    }
}
