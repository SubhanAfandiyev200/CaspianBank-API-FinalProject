using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class ReportServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public ReportServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithCustomerAsync<T>(Func<IReportService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IReportService>());
        }

        private async Task<T> WithStaffAsync<T>(Func<IReportAdminService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IReportAdminService>());
        }

        // Göndərən müştəridən alıcıya köçürmə: göndərənin çıxış əməliyyatının nömrəsini qaytarır
        private async Task<(Transaction Out, TransferReceiptDto Receipt)> TransferAsync(CustomerKit sender, CustomerKit recipient, decimal amount = 50m)
        {
            using var scope = _app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(sender.User.Id, new TransferDto
            {
                FromCardId = sender.Standard.Id,
                ToCardNumber = recipient.Standard.CardNumber,
                Amount = amount,
                RequestId = Guid.NewGuid().ToString()
            });
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var row = (await _app.TransactionsAsync(sender.Standard.Id)).Single(t => t.Reference == result.Data!.Reference && t.Type == TransactionType.TransferOut);
            return (row, result.Data!);
        }

        private static ReportTransactionDto Report(int transactionId, string reason = "I did not make this payment")
        {
            return new ReportTransactionDto { TransactionId = transactionId, Reason = reason };
        }

        private async Task<MyReportDto> ReportedAsync(CustomerKit sender, int transactionId)
        {
            var result = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(transactionId)));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            return result.Data!;
        }

        // ---------- Müştəri ----------

        [Fact]
        public async Task A_customer_can_report_money_that_left_their_card_and_staff_are_told()
        {
            var security = await _app.NewStaffAsync(Roles.Security);
            var support = await _app.NewStaffAsync(Roles.CustomerSupport);
            var sender = await _app.NewKitAsync(500m, "Aysel", "Mammadova");
            var recipient = await _app.NewKitAsync();
            var (row, receipt) = await TransferAsync(sender, recipient, 75m);

            var result = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id, "  I never sent this money  ")));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("Pending", result.Data!.Status);
            Assert.Equal(receipt.Reference, result.Data.TransactionReference);
            Assert.Equal(75m, result.Data.Amount);
            Assert.Equal("I never sent this money", result.Data.Reason);
            var notice = Assert.Single(await _app.NotificationsAsync(security.Id), n => n.Title == "Suspicious transaction reported");
            Assert.Contains("Aysel Mammadova reported " + receipt.Reference + " (75.00 AZN).", notice.Message);
            Assert.Equal("/Admin/Reports", notice.LinkUrl);
            Assert.DoesNotContain(await _app.NotificationsAsync(support.Id), n => n.Title == "Suspicious transaction reported");
        }

        [Fact]
        public async Task The_reason_must_be_between_ten_and_five_hundred_characters()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var (row, _) = await TransferAsync(sender, recipient);

            var tooShort = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id, " too short ")));
            var tooLong = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id, new string('x', 501))));
            var none = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, new ReportTransactionDto { TransactionId = row.Id, Reason = null }));
            var edge = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id, new string('y', 10))));

            Assert.Equal("Tell us what looks wrong (at least 10 characters).", tooShort.Errors[0]);
            Assert.Equal("The reason can be at most 500 characters.", tooLong.Errors[0]);
            Assert.False(none.IsSuccess);
            Assert.True(edge.IsSuccess);
        }

        [Fact]
        public async Task Only_own_outgoing_transactions_can_be_reported_and_only_once()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var stranger = await _app.NewKitAsync();
            var (row, _) = await TransferAsync(sender, recipient);
            var incoming = (await _app.TransactionsAsync(recipient.Standard.Id)).Single(t => t.Type == TransactionType.TransferIn);

            var foreign = await WithCustomerAsync(s => s.ReportAsync(stranger.User.Id, Report(row.Id)));
            var income = await WithCustomerAsync(s => s.ReportAsync(recipient.User.Id, Report(incoming.Id)));
            var unknown = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(999999)));
            var first = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id)));
            var twice = await WithCustomerAsync(s => s.ReportAsync(sender.User.Id, Report(row.Id)));
            var ghost = await WithCustomerAsync(s => s.ReportAsync("nobody", Report(row.Id)));

            Assert.Equal("We couldn't find this transaction on your cards.", foreign.Errors[0]);
            Assert.Equal("Only money that left your card can be reported.", income.Errors[0]);
            Assert.Equal("We couldn't find this transaction on your cards.", unknown.Errors[0]);
            Assert.True(first.IsSuccess);
            Assert.Equal("You already reported this transaction.", twice.Errors[0]);
            Assert.True(ghost.IsNotFound);
        }

        [Fact]
        public async Task My_reports_list_shows_only_mine_newest_first()
        {
            var sender = await _app.NewKitAsync(1000m);
            var other = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            var (first, _) = await TransferAsync(sender, recipient, 10m);
            await Task.Delay(30);
            var (second, _) = await TransferAsync(sender, recipient, 20m);
            var (foreign, _) = await TransferAsync(other, recipient, 30m);
            await ReportedAsync(sender, first.Id);
            await Task.Delay(30);
            await ReportedAsync(sender, second.Id);
            await ReportedAsync(other, foreign.Id);

            var mine = (await WithCustomerAsync(s => s.GetMineAsync(sender.User.Id))).ToList();

            Assert.Equal(new[] { 20m, 10m }, mine.Select(r => r.Amount).ToArray());
        }

        // ---------- İşçi: siyahı ----------

        [Fact]
        public async Task The_staff_list_proposes_freezing_the_receiver_when_the_money_went_to_a_customer()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync(0m, "Rauf", "Quliyev");
            var (row, receipt) = await TransferAsync(sender, recipient);
            await ReportedAsync(sender, row.Id);

            var page = await WithStaffAsync(s => s.GetPageAsync(new ReportFilterDto { Search = receipt.Reference }));

            var item = Assert.Single(page.Data!.Items);
            Assert.Equal("Freeze the account of Rauf Quliyev (the receiver).", item.ProposedAction);
            Assert.Equal("Pending", item.Status);
            Assert.Equal(sender.User.Email, item.ReporterEmail);
        }

        [Fact]
        public async Task The_staff_list_proposes_blocking_the_card_when_there_is_no_receiver()
        {
            var sender = await _app.NewKitAsync(500m);
            var fee = await _app.QueryAsync(async db =>
            {
                var transaction = new Transaction { CardId = sender.Standard.Id, Type = TransactionType.BillPayment, IsIncome = false, Amount = 5m, BalanceAfter = 495m, Description = "Some bill", Reference = "BP-NOREC" + TestData.Next() };
                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();
                return transaction;
            });
            await ReportedAsync(sender, fee.Id);

            var page = await WithStaffAsync(s => s.GetPageAsync(new ReportFilterDto { Search = fee.Reference }));

            Assert.Equal("Block the card Standard •••• " + sender.Standard.CardNumber[^4..] + ".", Assert.Single(page.Data!.Items).ProposedAction);
        }

        [Fact]
        public async Task The_staff_list_says_when_the_receiver_is_already_frozen_or_the_card_is_cashback()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync(0m, "Frozen", "Person");
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == recipient.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var (row, receipt) = await TransferAsync(sender, recipient);
            await ReportedAsync(sender, row.Id);
            var cashbackRow = await _app.QueryAsync(async db =>
            {
                var transaction = new Transaction { CardId = sender.Cashback.Id, Type = TransactionType.BillPayment, IsIncome = false, Amount = 1m, BalanceAfter = 0m, Description = "Cashback spend", Reference = "BP-CB" + TestData.Next() };
                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();
                return transaction;
            });
            await ReportedAsync(sender, cashbackRow.Id);

            var frozen = await WithStaffAsync(s => s.GetPageAsync(new ReportFilterDto { Search = receipt.Reference }));
            var cashback = await WithStaffAsync(s => s.GetPageAsync(new ReportFilterDto { Search = cashbackRow.Reference }));

            Assert.Equal("The account of Frozen Person is already frozen.", Assert.Single(frozen.Data!.Items).ProposedAction);
            Assert.Equal("No automatic step (Cashback card).", Assert.Single(cashback.Data!.Items).ProposedAction);
        }

        [Fact]
        public async Task The_list_filters_by_status_and_validates_the_filter()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var sender = await app.NewKitAsync(1000m);
                var recipient = await app.NewKitAsync();
                async Task<(Transaction Out, TransferReceiptDto Receipt)> Send(decimal amount)
                {
                    using var scope = app.NewScope();
                    var result = await scope.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = amount, RequestId = Guid.NewGuid().ToString() });
                    var row = (await app.TransactionsAsync(sender.Standard.Id)).Single(t => t.Reference == result.Data!.Reference && t.Type == TransactionType.TransferOut);
                    return (row, result.Data!);
                }

                var ids = new List<int>();
                foreach (var amount in new[] { 10m, 20m, 30m })
                {
                    var (row, _) = await Send(amount);
                    using var scope = app.NewScope();
                    var reported = await scope.ServiceProvider.GetRequiredService<IReportService>().ReportAsync(sender.User.Id, Report(row.Id));
                    ids.Add(reported.Data!.Id);
                }

                using var staffScope = app.NewScope();
                var staff = staffScope.ServiceProvider.GetRequiredService<IReportAdminService>();
                await staff.ResolveAsync("Tural", ids[0], new ResolveReportDto { Outcome = "Review" });
                await staff.ResolveAsync("Tural", ids[1], new ResolveReportDto { Outcome = "Dismiss", Note = "fine" });

                var all = await staff.GetPageAsync(new ReportFilterDto());
                Assert.Equal(3, all.Data!.Total);
                Assert.Equal(1, all.Data.PendingCount);
                Assert.Equal(2, all.Data.ResolvedThisWeek);
                Assert.Equal("Pending", all.Data.Items[0].Status);          // baxılmamış əvvəldə
                Assert.Equal(1, (await staff.GetPageAsync(new ReportFilterDto { Status = "pending" })).Data!.Total);
                Assert.Equal(1, (await staff.GetPageAsync(new ReportFilterDto { Status = "Reviewed" })).Data!.Total);
                Assert.Equal(1, (await staff.GetPageAsync(new ReportFilterDto { Status = "Dismissed" })).Data!.Total);
                Assert.Equal("Unknown status.", (await staff.GetPageAsync(new ReportFilterDto { Status = "Gone" })).Errors[0]);
                var clamped = await staff.GetPageAsync(new ReportFilterDto { Page = -1, PageSize = 100000 });
                Assert.Equal(1, clamped.Data!.Page);
                Assert.Equal(100, clamped.Data.PageSize);
            }
        }

        // ---------- İşçi: qərar ----------

        [Fact]
        public async Task Marking_a_report_reviewed_tells_the_customer_and_records_who_and_when()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var (row, receipt) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync("Tural Hesenov", report.Id, new ResolveReportDto { Outcome = "Review", Note = "  checked  " }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Reviewed", result.Data!.Status);
            Assert.Equal("Tural Hesenov", result.Data.ResolvedByName);
            Assert.Equal("checked", result.Data.ResolutionNote);
            Assert.Null(result.Data.ActionTaken);
            Assert.NotNull(result.Data.ResolvedAt);
            var notice = Assert.Single(await _app.NotificationsAsync(sender.User.Id), n => n.Title == "Your report was reviewed");
            Assert.Equal("We reviewed your report about " + receipt.Reference + ".", notice.Message);
            Assert.Equal("/App/History", notice.LinkUrl);
        }

        [Fact]
        public async Task Dismissing_says_no_problem_was_found()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var (row, receipt) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync(" ", report.Id, new ResolveReportDto { Outcome = "Dismiss" }));

            Assert.Equal("Dismissed", result.Data!.Status);
            Assert.Equal("Staff", result.Data.ResolvedByName);
            var notice = Assert.Single(await _app.NotificationsAsync(sender.User.Id), n => n.Title == "Your report was reviewed");
            Assert.Equal("We reviewed your report about " + receipt.Reference + " and found no problem.", notice.Message);
        }

        [Fact]
        public async Task Taking_action_on_a_transfer_to_a_customer_freezes_the_receiver()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync(0m, "Orxan", "Scammer");
            var (row, receipt) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync("Tural Hesenov", report.Id, new ResolveReportDto { Outcome = "Action", Note = "Receiver is a scammer" }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("ActionTaken", result.Data!.Status);
            Assert.Equal("The account of Orxan Scammer was frozen.", result.Data.ActionTaken);
            var user = await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == recipient.User.Id));
            Assert.True(user.IsRestricted);
            Assert.Equal("Report " + receipt.Reference + ": Receiver is a scammer", user.RestrictionReason);
            Assert.Equal("Tural Hesenov", user.RestrictedBy);
            var notice = Assert.Single(await _app.NotificationsAsync(sender.User.Id), n => n.Title == "Your report was reviewed");
            Assert.Equal("We reviewed your report about " + receipt.Reference + " and took action.", notice.Message);
            Assert.False(await _app.QueryAsync(db => db.Users.AsNoTracking().Where(u => u.Id == sender.User.Id).Select(u => u.IsRestricted).FirstAsync()));
        }

        [Fact]
        public async Task Taking_action_when_the_receiver_is_already_frozen_only_says_so()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync(0m, "Already", "Frozen");
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == recipient.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true).SetProperty(u => u.RestrictionReason, "earlier"));
                return 0;
            });
            var (row, _) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync("Staff", report.Id, new ResolveReportDto { Outcome = "Action", Note = "already done" }));

            Assert.Equal("The account of Already Frozen was already frozen.", result.Data!.ActionTaken);
            Assert.Equal("earlier", (await _app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == recipient.User.Id))).RestrictionReason);
        }

        [Fact]
        public async Task Taking_action_without_a_receiver_blocks_the_reported_card()
        {
            var sender = await _app.NewKitAsync(500m);
            var bill = await _app.QueryAsync(async db =>
            {
                var transaction = new Transaction { CardId = sender.Standard.Id, Type = TransactionType.BillPayment, IsIncome = false, Amount = 5m, BalanceAfter = 495m, Description = "Some bill", Reference = "BP-BLOCK" + TestData.Next() };
                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();
                return transaction;
            });
            var report = await ReportedAsync(sender, bill.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync("Staff", report.Id, new ResolveReportDto { Outcome = "Action", Note = "card is compromised" }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("The card Standard •••• " + sender.Standard.CardNumber[^4..] + " was blocked.", result.Data!.ActionTaken);
            Assert.True(await _app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.Id == sender.Standard.Id).Select(c => c.IsBlocked).FirstAsync()));
        }

        [Fact]
        public async Task Taking_action_on_a_cashback_card_transaction_without_a_receiver_is_impossible()
        {
            var sender = await _app.NewKitAsync(500m);
            var spend = await _app.QueryAsync(async db =>
            {
                var transaction = new Transaction { CardId = sender.Cashback.Id, Type = TransactionType.BillPayment, IsIncome = false, Amount = 1m, BalanceAfter = 0m, Description = "Cashback spend", Reference = "BP-CBX" + TestData.Next() };
                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();
                return transaction;
            });
            var report = await ReportedAsync(sender, spend.Id);

            var result = await WithStaffAsync(s => s.ResolveAsync("Staff", report.Id, new ResolveReportDto { Outcome = "Action", Note = "try to block it" }));

            Assert.False(result.IsSuccess);
            Assert.Contains("the Cashback card can't be blocked", result.Errors[0]);
            Assert.Equal("Pending", (await _app.QueryAsync(db => db.TransactionReports.AsNoTracking().FirstAsync(r => r.Id == report.Id))).Status.ToString());
        }

        [Fact]
        public async Task Resolve_checks_the_outcome_the_note_and_that_the_report_is_still_pending()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var (row, _) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            Assert.Equal("Choose what to do with this report.", (await WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Maybe" }))).Errors[0]);
            Assert.Equal("The note can be at most 300 characters.", (await WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Review", Note = new string('n', 301) }))).Errors[0]);
            Assert.Equal("Write why action is taken (at least 5 characters).", (await WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Action", Note = "abc" }))).Errors[0]);
            Assert.True((await WithStaffAsync(s => s.ResolveAsync("S", 999999, new ResolveReportDto { Outcome = "Review" }))).IsNotFound);

            Assert.True((await WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Review" }))).IsSuccess);
            Assert.Equal("This report was already handled.", (await WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Dismiss" }))).Errors[0]);
        }

        [Fact]
        public async Task A_report_is_not_double_handled_when_two_staff_resolve_it_at_once()
        {
            var sender = await _app.NewKitAsync(500m);
            var recipient = await _app.NewKitAsync();
            var (row, _) = await TransferAsync(sender, recipient);
            var report = await ReportedAsync(sender, row.Id);

            var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => WithStaffAsync(s => s.ResolveAsync("S", report.Id, new ResolveReportDto { Outcome = "Review" }))));

            Assert.True(results.Count(r => r.IsSuccess) >= 1);
            Assert.Single(await _app.NotificationsAsync(sender.User.Id), n => n.Title == "Your report was reviewed");
        }
    }
}
