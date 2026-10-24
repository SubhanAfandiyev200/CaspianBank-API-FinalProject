using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.Bank;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.DTOs.Transfers;
using Service.Services.Interfaces;
using System.Globalization;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class DashboardAndTreasuryTests
    {
        private static async Task<T> WithAsync<T, TService>(TestApp app, Func<TService, Task<T>> action) where TService : notnull
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<TService>());
        }

        private static async Task<TestApp> NewBankAsync(Action<OwnBank>? change = null)
        {
            var app = await TestApp.CreateAsync();
            await app.QueryAsync(async db =>
            {
                var bank = await TestData.EnsureOwnBankAsync(db);
                if (change is not null)
                {
                    change(bank);
                    await db.SaveChangesAsync();
                }
                return 0;
            });
            return app;
        }

        private static string ThisMonth()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        private static async Task TransferAsync(TestApp app, CustomerKit sender, CustomerKit recipient, decimal amount)
        {
            var result = await WithAsync<Service.Helpers.Responses.ServiceResult<TransferReceiptDto>, ITransferService>(app, s => s.TransferAsync(sender.User.Id, new TransferDto
            {
                FromCardId = sender.Standard.Id,
                ToCardNumber = recipient.Standard.CardNumber,
                Amount = amount,
                RequestId = Guid.NewGuid().ToString()
            }));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
        }

        // ---------- Dashboard ----------

        [Fact]
        public async Task An_empty_bank_shows_only_the_operating_cost_as_a_loss_and_a_healthy_reserve()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var result = await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null));

                Assert.True(result.IsSuccess);
                var dashboard = result.Data!;
                Assert.Equal(ThisMonth(), dashboard.Period);
                Assert.Equal(0, dashboard.TotalUsers);
                Assert.Equal(0m, dashboard.TotalIncome);
                Assert.Equal(150m, dashboard.OperatingCosts);
                Assert.Equal(-150m, dashboard.ProfitBeforeTax);
                Assert.Equal(0m, dashboard.ProfitTax);                     // zərər olanda vergi yoxdur
                Assert.Equal(150m, dashboard.TotalExpenses);
                Assert.Equal(-150m, dashboard.NetProfit);
                Assert.Equal(2400000m, dashboard.OwnFunds);
                Assert.Equal(0m, dashboard.RequiredReserve);
                Assert.Null(dashboard.CoverageRatio);
                Assert.True(dashboard.ReserveHealthy);
                Assert.Equal(12, dashboard.Months.Count);
                Assert.Equal(ThisMonth(), dashboard.Months[0].Value);
            }
        }

        [Fact]
        public async Task Income_comes_from_commissions_and_card_fees_and_profit_is_taxed()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var sender = await app.NewKitAsync(2000m);
                var recipient = await app.NewKitAsync();
                await TransferAsync(app, sender, recipient, 600m);                    // komissiya 1.00
                await WithAsync<Service.Helpers.Responses.ServiceResult<CardDto>, ICardService>(app, s => s.AddCardAsync(sender.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = sender.Standard.Id }));       // haqq 40

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;

                Assert.Equal(1m, dashboard.TransferCommissions);
                Assert.Equal(40m, dashboard.CardOpeningFees);
                Assert.Equal(41m, dashboard.TotalIncome);
                Assert.Equal(41m, dashboard.ProfitBeforeTax);
                Assert.Equal(8.20m, dashboard.ProfitTax);                              // 20%
                Assert.Equal(8.20m, dashboard.TotalExpenses);
                Assert.Equal(32.80m, dashboard.NetProfit);
                Assert.Equal(8.20m, dashboard.QuarterTaxAccrued);
                Assert.Equal(2, dashboard.TotalUsers);
                Assert.Equal(2, dashboard.NewUsers);
            }
        }

        [Fact]
        public async Task Cashback_and_deposit_interest_are_expenses_that_lower_the_taxable_profit()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var kit = await app.NewKitAsync(1000m);
                await app.QueryAsync(async db =>
                {
                    db.Transactions.Add(new Transaction { CardId = kit.Cashback.Id, Type = TransactionType.Cashback, IsIncome = true, Amount = 5m, BalanceAfter = 5m, Description = "Cashback", Reference = "CB-T1" });
                    db.Transactions.Add(new Transaction { CardId = kit.Standard.Id, Type = TransactionType.CardFee, IsIncome = false, Amount = 100m, BalanceAfter = 900m, Description = "Fee", Reference = "FE-T1" });
                    var deposit = new Deposit { UserId = kit.User.Id, TermMonths = 6, AnnualRate = 12m, Principal = 1000m, ExpectedInterest = 60m, StartedAt = DateTime.UtcNow, MaturesAt = DateTime.UtcNow.AddMonths(6), SourceCardId = kit.Standard.Id, PayoutCardId = kit.Standard.Id, Reference = "DP-T1" };
                    db.Deposits.Add(deposit);
                    await db.SaveChangesAsync();
                    db.DepositInterestPostings.Add(new DepositInterestPosting { DepositId = deposit.Id, Number = 1, Amount = 10m });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;

                Assert.Equal(100m, dashboard.TotalIncome);
                Assert.Equal(5m, dashboard.CashbackPaid);
                Assert.Equal(10m, dashboard.DepositInterestPaid);
                Assert.Equal(85m, dashboard.ProfitBeforeTax);                          // 100 - 5 - 10
                Assert.Equal(17m, dashboard.ProfitTax);
                Assert.Equal(32m, dashboard.TotalExpenses);                            // 5 + 10 + 17
                Assert.Equal(68m, dashboard.NetProfit);
                Assert.Equal(1000m, dashboard.TermDeposits);
                Assert.Equal(60m, dashboard.TermDepositInterestOwed);
            }
        }

        [Fact]
        public async Task The_required_reserve_is_a_share_of_card_money_and_deposits_and_coverage_is_the_ratio()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var kit = await app.NewKitAsync(10000m);
                await app.QueryAsync(async db =>
                {
                    db.Deposits.Add(new Deposit { UserId = kit.User.Id, TermMonths = 6, AnnualRate = 12m, Principal = 5000m, ExpectedInterest = 300m, StartedAt = DateTime.UtcNow, MaturesAt = DateTime.UtcNow.AddMonths(6), SourceCardId = kit.Standard.Id, PayoutCardId = kit.Standard.Id, Reference = "DP-T2" });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;

                Assert.Equal(10000m, dashboard.CustomerDeposits);
                Assert.Equal(5000m, dashboard.TermDeposits);
                Assert.Equal(1500m, dashboard.RequiredReserve);                        // 10% x 15000
                Assert.Equal(2400000m, dashboard.OwnFunds);
                Assert.Equal(1600m, dashboard.CoverageRatio);                          // 2 400 000 / 1 500
                Assert.True(dashboard.ReserveHealthy);
            }
        }

        [Fact]
        public async Task The_reserve_is_unhealthy_when_the_treasury_is_below_what_is_needed()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var kit = await app.NewKitAsync(10000m);
                await app.QueryAsync(async db =>
                {
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.OperatingCost, Amount = -2399500m, Description = "drain", Reference = "T-DRAIN" });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;

                Assert.Equal(500m, dashboard.OwnFunds);
                Assert.Equal(1000m, dashboard.RequiredReserve);
                Assert.False(dashboard.ReserveHealthy);
                Assert.Equal(0.5m, dashboard.CoverageRatio);
            }
        }

        [Fact]
        public async Task Counts_show_pending_loans_tickets_and_restricted_accounts_and_exclude_staff()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var staff = await app.NewStaffAsync();
                var borrower = await app.NewBorrowerAsync();
                await app.ApplyAsync(borrower);
                var frozen = await app.NewKitAsync();
                await app.QueryAsync(async db =>
                {
                    await db.Users.Where(u => u.Id == frozen.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                    db.SupportTickets.Add(new SupportTicket { UserId = frozen.User.Id, Subject = "Help", Status = TicketStatus.Pending });
                    db.SupportTickets.Add(new SupportTicket { UserId = frozen.User.Id, Subject = "Done", Status = TicketStatus.Closed });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;

                Assert.Equal(2, dashboard.TotalUsers);                // staff sayılmır
                Assert.Equal(1, dashboard.PendingLoans);
                Assert.Equal(1, dashboard.PendingTickets);
                Assert.Equal(1, dashboard.RestrictedAccounts);
                Assert.NotNull(staff);
            }
        }

        [Fact]
        public async Task The_month_must_be_valid_and_not_in_the_future()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var bad = await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync("october"));
                var future = await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync("2099-01"));
                var past = await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync("2026-01"));

                Assert.Equal("Choose a month like 2026-10.", bad.Errors[0]);
                Assert.Equal("That month has not started yet.", future.Errors[0]);
                Assert.True(past.IsSuccess);
                Assert.Equal("2026-01", past.Data!.Period);
                Assert.Equal("January 2026", past.Data.PeriodLabel);
            }
        }

        [Fact]
        public async Task The_change_percent_compares_with_the_previous_month_and_is_empty_without_a_base()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var kit = await app.NewKitAsync(1000m);
                var today = LoanEngine.BakuToday(DateTime.UtcNow);
                var thisMonth = new DateTime(today.Year, today.Month, 1).AddHours(-4).AddDays(1);
                var lastMonth = thisMonth.AddMonths(-1);
                await app.QueryAsync(async db =>
                {
                    db.Transactions.Add(new Transaction { CardId = kit.Standard.Id, Type = TransactionType.CardFee, IsIncome = false, Amount = 100m, BalanceAfter = 900m, Description = "Fee", Reference = "FE-PREV", CreatedAt = DateTime.SpecifyKind(lastMonth, DateTimeKind.Utc) });
                    db.Transactions.Add(new Transaction { CardId = kit.Standard.Id, Type = TransactionType.CardFee, IsIncome = false, Amount = 150m, BalanceAfter = 750m, Description = "Fee", Reference = "FE-NOW", CreatedAt = DateTime.SpecifyKind(thisMonth, DateTimeKind.Utc) });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var dashboard = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(null))).Data!;
                var previous = (await WithAsync<Service.Helpers.Responses.ServiceResult<BankDashboardDto>, IBankDashboardService>(app, s => s.GetAsync(lastMonth.AddHours(4).ToString("yyyy-MM", CultureInfo.InvariantCulture)))).Data!;

                Assert.Equal(150m, dashboard.TotalIncome);
                Assert.Equal(50m, dashboard.IncomeChangePercent);                      // 100 -> 150
                Assert.Equal(100m, previous.TotalIncome);
                Assert.Null(previous.IncomeChangePercent);                             // ondan əvvəlki ay boşdur
            }
        }

        [Fact]
        public async Task Month_costs_return_the_operating_cost_and_tax_or_nothing_without_a_bank()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var none = await WithAsync<(decimal OperatingCosts, decimal Tax)?, IBankDashboardService>(app, s => s.GetMonthCostsAsync(new DateTime(2026, 9, 1)));
                Assert.Null(none);

                await app.QueryAsync(async db =>
                {
                    var bank = await TestData.EnsureOwnBankAsync(db);
                    bank.OperatingCostMonthly = 10m;
                    await db.SaveChangesAsync();
                    return 0;
                });
                var costs = await WithAsync<(decimal OperatingCosts, decimal Tax)?, IBankDashboardService>(app, s => s.GetMonthCostsAsync(new DateTime(2026, 9, 17)));

                Assert.Equal(10m, costs!.Value.OperatingCosts);
                Assert.Equal(0m, costs.Value.Tax);
            }
        }

        // ---------- Xəzinə ----------

        [Fact]
        public async Task The_treasury_page_shows_balance_reserve_and_what_is_available_for_loans()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(20000m);

                var page = (await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryPageDto>, ITreasuryService>(app, s => s.GetPageAsync(new TreasuryFilterDto()))).Data!;

                Assert.Equal(2400000m, page.Balance);
                Assert.Equal(20000m, page.CustomerMoney);
                Assert.Equal(10m, page.ReservePercent);
                Assert.Equal(2000m, page.RequiredReserve);
                Assert.Equal(2398000m, page.AvailableForLoans);
                Assert.False(page.InDebt);
                Assert.Equal(DateTime.UtcNow.AddHours(4).ToString("MMMM yyyy", CultureInfo.InvariantCulture), page.MonthLabel);
                Assert.Equal("Capital", Assert.Single(page.Items).Kind);
                Assert.NotNull(kit);
            }
        }

        [Fact]
        public async Task A_negative_treasury_is_in_debt_and_leaves_nothing_for_loans()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                await app.QueryAsync(async db =>
                {
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.OperatingCost, Amount = -2500000m, Description = "big loss", Reference = "T-DEBT" });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var page = (await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryPageDto>, ITreasuryService>(app, s => s.GetPageAsync(new TreasuryFilterDto()))).Data!;

                Assert.Equal(-100000m, page.Balance);
                Assert.True(page.InDebt);
                Assert.Equal(0m, page.AvailableForLoans);
            }
        }

        [Fact]
        public async Task The_treasury_journal_shows_the_running_balance_newest_first()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var sender = await app.NewKitAsync(2000m);
                var recipient = await app.NewKitAsync();
                await TransferAsync(app, sender, recipient, 600m);                        // +1.00 komissiya
                await app.QueryAsync(async db =>
                {
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.Cashback, Amount = -0.5m, Description = "cb", Reference = "T-CB" });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var page = (await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryPageDto>, ITreasuryService>(app, s => s.GetPageAsync(new TreasuryFilterDto()))).Data!;

                Assert.Equal(3, page.Total);
                Assert.Equal(new[] { "Cashback", "Commission", "Capital" }, page.Items.Select(i => i.Kind).ToArray());
                Assert.Equal(new[] { 2400000.5m, 2400001m, 2400000m }, page.Items.Select(i => i.BalanceAfter).ToArray());
                Assert.Equal(1m, page.MonthIn);                                        // kapital gəlir sayılmır
                Assert.Equal(0.5m, page.MonthOut);
            }
        }

        [Fact]
        public async Task The_treasury_journal_filters_by_kind_and_baku_dates_and_validates_input()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                await app.QueryAsync(async db =>
                {
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.Commission, Amount = 1m, Description = "a", Reference = "T-A", CreatedAt = new DateTime(2026, 3, 10, 21, 0, 0, DateTimeKind.Utc) });
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.CardFee, Amount = 5m, Description = "b", Reference = "T-B", CreatedAt = new DateTime(2026, 3, 10, 19, 0, 0, DateTimeKind.Utc) });
                    await db.SaveChangesAsync();
                    return 0;
                });
                Task<Service.Helpers.Responses.ServiceResult<TreasuryPageDto>> Get(TreasuryFilterDto filter) => WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryPageDto>, ITreasuryService>(app, s => s.GetPageAsync(filter));

                Assert.Equal(1, (await Get(new TreasuryFilterDto { Kind = "cardfee" })).Data!.Total);
                Assert.Equal("T-A", Assert.Single((await Get(new TreasuryFilterDto { From = "2026-03-11", To = "2026-03-11" })).Data!.Items).Reference);
                Assert.Equal("T-B", Assert.Single((await Get(new TreasuryFilterDto { From = "2026-03-10", To = "2026-03-10" })).Data!.Items).Reference);
                Assert.Equal("Unknown entry type.", (await Get(new TreasuryFilterDto { Kind = "Magic" })).Errors[0]);
                Assert.Equal("Unknown entry type.", (await Get(new TreasuryFilterDto { Kind = "99" })).Errors[0]);
                Assert.Equal("Choose dates like 2026-10-17.", (await Get(new TreasuryFilterDto { From = "10/03/2026" })).Errors[0]);
                Assert.Equal("The start date can't be after the end date.", (await Get(new TreasuryFilterDto { From = "2026-03-12", To = "2026-03-01" })).Errors[0]);
                var clamped = await Get(new TreasuryFilterDto { Page = -2, PageSize = 100000 });
                Assert.Equal(1, clamped.Data!.Page);
                Assert.Equal(100, clamped.Data.PageSize);
            }
        }

        // ---------- Ayın bağlanması ----------

        [Fact]
        public async Task Closing_a_month_writes_the_operating_cost_and_the_tax_once()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 100m);
            await using (app)
            {
                var kit = await app.NewKitAsync();
                await app.QueryAsync(async db =>
                {
                    db.Transactions.Add(new Transaction { CardId = kit.Standard.Id, Type = TransactionType.CardFee, IsIncome = false, Amount = 400m, BalanceAfter = 0m, Description = "Fee", Reference = "FE-CLOSE", CreatedAt = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc) });
                    await db.SaveChangesAsync();
                    return 0;
                });

                var first = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("2026-01"));
                var second = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("2026-01"));

                Assert.True(first.IsSuccess);
                Assert.False(first.Data!.AlreadyClosed);
                Assert.Equal(100m, first.Data.OperatingCost);
                Assert.Equal(60m, first.Data.ProfitTax);                              // (400 - 100) x 20%
                Assert.True(second.Data!.AlreadyClosed);

                var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference.StartsWith("CLOSE-2026-01")).ToListAsync());
                Assert.Equal(-100m, entries.Single(e => e.Kind == TreasuryEntryKind.OperatingCost).Amount);
                Assert.Equal(-60m, entries.Single(e => e.Kind == TreasuryEntryKind.ProfitTax).Amount);
                Assert.Equal(2, entries.Count);
            }
        }

        [Fact]
        public async Task Closing_a_month_with_no_costs_still_marks_it_closed()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 0m);
            await using (app)
            {
                var result = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("2026-02"));
                var again = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("2026-02"));

                Assert.True(result.IsSuccess);
                Assert.Equal(0m, result.Data!.OperatingCost);
                Assert.True(again.Data!.AlreadyClosed);
                var entry = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().SingleAsync(t => t.Reference == "CLOSE-2026-02-OPEX"));
                Assert.Equal(0m, entry.Amount);
            }
        }

        [Fact]
        public async Task Only_a_finished_month_in_the_right_format_can_be_closed()
        {
            var app = await NewBankAsync();
            await using (app)
            {
                var bad = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("January"));
                var empty = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync(""));
                var current = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync(ThisMonth()));
                var future = await WithAsync<Service.Helpers.Responses.ServiceResult<TreasuryCloseDto>, ITreasuryService>(app, s => s.CloseMonthAsync("2099-01"));

                Assert.Equal("Choose a month like 2026-10.", bad.Errors[0]);
                Assert.Equal("Choose a month like 2026-10.", empty.Errors[0]);
                Assert.Equal("Only a finished month can be closed.", current.Errors[0]);
                Assert.Equal("Only a finished month can be closed.", future.Errors[0]);
            }
        }

        [Fact]
        public async Task The_automatic_close_covers_the_finished_months_from_october_2026_once()
        {
            var app = await NewBankAsync(b => b.OperatingCostMonthly = 10m);
            await using (app)
            {
                var today = LoanEngine.BakuToday(DateTime.UtcNow);
                var previous = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                var expected = 0;
                for (var m = new DateTime(2026, 10, 1); m <= previous; m = m.AddMonths(1))
                {
                    expected++;
                }

                var first = await WithAsync<int, ITreasuryService>(app, s => s.CloseDueMonthsAsync());
                var second = await WithAsync<int, ITreasuryService>(app, s => s.CloseDueMonthsAsync());

                Assert.Equal(expected, first);
                Assert.Equal(0, second);
            }
        }
    }
}
