using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.DTOs.Users;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class UserAdminServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public UserAdminServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IUserAdminService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IUserAdminService>());
        }

        // Axtarışda tapılması üçün təkrarsız ad
        private async Task<CustomerKit> NamedAsync(string marker, decimal balance = 0m)
        {
            return await _app.NewKitAsync(balance, "N" + marker, "S" + marker);
        }

        private async Task<UserPageDto> SearchAsync(string text, string? status = null)
        {
            var page = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Search = text, Status = status, PageSize = 50 }));
            Assert.True(page.IsSuccess, string.Join(";", page.Errors));
            return page.Data!;
        }

        // ---------- Siyahı ----------

        [Fact]
        public async Task The_list_contains_customers_only_not_staff()
        {
            var staff = await _app.NewStaffAsync(Roles.Admin);
            var kit = await NamedAsync("only" + TestData.Next());

            var page = await WithAsync(s => s.GetPageAsync(new UserFilterDto { PageSize = 50 }));

            Assert.DoesNotContain(page.Data!.Items, u => u.Id == staff.Id);
            Assert.Contains(page.Data.Items, u => u.Id == kit.User.Id);
        }

        [Fact]
        public async Task The_list_shows_card_count_and_total_balance()
        {
            var kit = await NamedAsync("bal" + TestData.Next(), 150m);
            await _app.SetBalanceAsync(kit.Cashback.Id, 25m);

            var item = Assert.Single((await SearchAsync(kit.User.Email!)).Items);

            Assert.Equal(kit.User.Id, item.Id);
            Assert.Equal(2, item.CardCount);
            Assert.Equal(175m, item.TotalBalance);
            Assert.Equal(kit.Customer.Fin, item.Fin);
            Assert.False(item.IsRestricted);
        }

        [Fact]
        public async Task Search_finds_by_name_email_phone_fin_id_card_and_card_number()
        {
            var kit = await NamedAsync("find" + TestData.Next());
            var phone = (await _app.QueryAsync(db => db.Users.AsNoTracking().Where(u => u.Id == kit.User.Id).Select(u => u.PhoneNumber).FirstAsync()))!;

            Assert.Contains(kit.User.Id, (await SearchAsync(kit.User.Name)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.User.Email!)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(phone)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.Customer.Fin)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.Customer.IdCardNumber)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.Standard.CardNumber)).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.Standard.CardNumber.Substring(4, 8))).Items.Select(i => i.Id));
            Assert.Contains(kit.User.Id, (await SearchAsync(kit.User.Name + " " + kit.User.Surname)).Items.Select(i => i.Id));
        }

        [Fact]
        public async Task Search_treats_wildcard_characters_as_plain_text()
        {
            var kit = await NamedAsync("wild" + TestData.Next());

            Assert.Empty((await SearchAsync("%%%%%%%%%%%")).Items);
            Assert.Empty((await SearchAsync("_____________")).Items);
            Assert.Empty((await SearchAsync("[a-z]zzzzzz")).Items);
            Assert.NotNull(kit);
        }

        [Fact]
        public async Task The_status_filter_splits_active_and_restricted_and_counts_restricted()
        {
            var active = await NamedAsync("act" + TestData.Next());
            var frozen = await NamedAsync("frz" + TestData.Next());
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == frozen.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });

            var restricted = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Status = "restricted", PageSize = 50 }));
            var activeOnly = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Status = "Active", PageSize = 50 }));

            Assert.Contains(restricted.Data!.Items, u => u.Id == frozen.User.Id);
            Assert.DoesNotContain(restricted.Data.Items, u => u.Id == active.User.Id);
            Assert.All(restricted.Data.Items, u => Assert.True(u.IsRestricted));
            Assert.DoesNotContain(activeOnly.Data!.Items, u => u.Id == frozen.User.Id);
            Assert.Contains(activeOnly.Data.Items, u => u.Id == active.User.Id);
            Assert.True(restricted.Data.RestrictedCount >= 1);
            Assert.Equal(restricted.Data.RestrictedCount, restricted.Data.TotalCount);
        }

        [Fact]
        public async Task An_unknown_status_is_refused_and_paging_is_clamped()
        {
            var bad = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Status = "Banned" }));
            var clamped = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Page = -4, PageSize = 100000 }));
            var tiny = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Page = 1, PageSize = 0 }));

            Assert.Equal("Choose Active or Restricted.", bad.Errors[0]);
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(50, clamped.Data.PageSize);
            Assert.Equal(1, tiny.Data!.PageSize);
            Assert.True(tiny.Data.Items.Count <= 1);
        }

        [Fact]
        public async Task The_list_is_ordered_by_name_and_pages_do_not_overlap()
        {
            await NamedAsync("aaa" + TestData.Next());
            await NamedAsync("bbb" + TestData.Next());

            var first = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Page = 1, PageSize = 2 }));
            var second = await WithAsync(s => s.GetPageAsync(new UserFilterDto { Page = 2, PageSize = 2 }));

            Assert.Equal(2, first.Data!.Items.Count);
            Assert.Empty(first.Data.Items.Select(i => i.Id).Intersect(second.Data!.Items.Select(i => i.Id)));
            var names = first.Data.Items.Select(i => i.FullName).ToList();
            Assert.Equal(names.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList(), names);
        }

        // ---------- Ətraflı məlumat ----------

        [Fact]
        public async Task The_detail_shows_cards_loans_activity_and_totals()
        {
            var sender = await _app.NewBorrowerAsync(3000m, 5000m);
            var recipient = await _app.NewKitAsync();
            await _app.ActiveLoanAsync(sender, 600m, 6);
            var transfer = await WithTransfer(sender, recipient, 40m);

            var detail = await WithAsync(s => s.GetDetailAsync(sender.User.Id));

            Assert.True(detail.IsSuccess);
            var data = detail.Data!;
            Assert.Equal(sender.User.Id, data.Id);
            Assert.Equal(sender.Customer.Fin, data.Fin);
            Assert.Equal(2, data.Cards.Count);
            Assert.Equal(sender.Standard.CardNumber[^4..], data.Cards.First(c => c.Tier == "Standard").Last4);
            var loan = Assert.Single(data.Loans);
            Assert.Equal("Active", loan.Status);
            Assert.Equal(600m, loan.Amount);
            Assert.Equal(40m, data.TotalSent);
            Assert.True(data.RecentActivity.Count >= 2);
            Assert.Contains(data.RecentActivity, a => a.Type == "TransferOut" && a.Amount == 40m && !a.IsIncome);
            Assert.Contains(data.RecentActivity, a => a.Type == "LoanDisbursement" && a.IsIncome);
            Assert.StartsWith("Standard ••••", data.RecentActivity.First(a => a.Type == "TransferOut").CardLabel);
            Assert.NotNull(transfer);
        }

        private async Task<TransferReceiptDto> WithTransfer(CustomerKit sender, CustomerKit recipient, decimal amount)
        {
            using var scope = _app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = amount, RequestId = Guid.NewGuid().ToString() });
            Assert.True(result.IsSuccess);
            return result.Data!;
        }

        [Fact]
        public async Task The_detail_is_not_found_for_staff_and_unknown_users()
        {
            var staff = await _app.NewStaffAsync(Roles.Admin);

            Assert.True((await WithAsync(s => s.GetDetailAsync(staff.Id))).IsNotFound);
            Assert.True((await WithAsync(s => s.GetDetailAsync("nobody"))).IsNotFound);
        }

        [Fact]
        public async Task A_customer_without_activity_has_an_empty_but_valid_detail()
        {
            var kit = await _app.NewKitAsync();

            var detail = await WithAsync(s => s.GetDetailAsync(kit.User.Id));

            Assert.True(detail.IsSuccess);
            Assert.Empty(detail.Data!.RecentActivity);
            Assert.Empty(detail.Data.Loans);
            Assert.Equal(0m, detail.Data.TotalSent);
            Assert.Equal(0, detail.Data.BillPaymentCount);
        }

        // ---------- Dondurma ----------

        [Fact]
        public async Task Restrict_marks_the_account_with_the_reason_who_and_when()
        {
            var kit = await _app.NewKitAsync();

            var result = await WithAsync(s => s.RestrictAsync("Nurlan Efendiyev", kit.User.Id, new RestrictUserDto { Reason = "  Suspicious logins  " }));

            Assert.True(result.IsSuccess);
            Assert.True(result.Data!.IsRestricted);
            Assert.Equal("Suspicious logins", result.Data.RestrictionReason);
            Assert.Equal("Nurlan Efendiyev", result.Data.RestrictedBy);
            Assert.InRange((DateTime.UtcNow - result.Data.RestrictedAt!.Value).TotalMinutes, -1, 1);
        }

        [Fact]
        public async Task Restrict_needs_a_reason_and_a_customer_that_is_not_restricted_yet()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.Admin);

            var noReason = await WithAsync(s => s.RestrictAsync("Staff", kit.User.Id, new RestrictUserDto { Reason = "abc" }));
            var onStaff = await WithAsync(s => s.RestrictAsync("Staff", staff.Id, new RestrictUserDto { Reason = "freeze admin" }));
            var unknown = await WithAsync(s => s.RestrictAsync("Staff", "nobody", new RestrictUserDto { Reason = "freeze nobody" }));
            await WithAsync(s => s.RestrictAsync("Staff", kit.User.Id, new RestrictUserDto { Reason = "first time" }));
            var twice = await WithAsync(s => s.RestrictAsync("Staff", kit.User.Id, new RestrictUserDto { Reason = "second time" }));

            Assert.False(noReason.IsSuccess);
            Assert.True(onStaff.IsNotFound);
            Assert.True(unknown.IsNotFound);
            Assert.Equal("This account is already restricted.", twice.Errors[0]);
        }

        [Fact]
        public async Task A_blank_staff_name_is_stored_as_staff()
        {
            var kit = await _app.NewKitAsync();

            var result = await WithAsync(s => s.RestrictAsync(" ", kit.User.Id, new RestrictUserDto { Reason = "blank name" }));

            Assert.Equal("Staff", result.Data!.RestrictedBy);
        }

        [Fact]
        public async Task Unrestrict_clears_everything_and_only_works_on_a_restricted_customer()
        {
            var kit = await _app.NewKitAsync();
            var staff = await _app.NewStaffAsync(Roles.Admin);
            await WithAsync(s => s.RestrictAsync("Staff", kit.User.Id, new RestrictUserDto { Reason = "to be cleared" }));

            var cleared = await WithAsync(s => s.UnrestrictAsync(kit.User.Id));
            var again = await WithAsync(s => s.UnrestrictAsync(kit.User.Id));

            Assert.True(cleared.IsSuccess);
            Assert.False(cleared.Data!.IsRestricted);
            Assert.Null(cleared.Data.RestrictionReason);
            Assert.Null(cleared.Data.RestrictedAt);
            Assert.Null(cleared.Data.RestrictedBy);
            Assert.Equal("This account is not restricted.", again.Errors[0]);
            Assert.True((await WithAsync(s => s.UnrestrictAsync(staff.Id))).IsNotFound);
            Assert.True((await WithAsync(s => s.UnrestrictAsync("nobody"))).IsNotFound);
        }

        [Fact]
        public async Task A_restricted_customer_cannot_transfer_until_unrestricted()
        {
            var kit = await _app.NewKitAsync(100m);
            var silver = await _app.NewCardAsync(kit.User.Id, CardTier.Silver);
            await WithAsync(s => s.RestrictAsync("Staff", kit.User.Id, new RestrictUserDto { Reason = "investigation" }));

            using (var scope = _app.NewScope())
            {
                var blocked = await scope.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 10m });
                Assert.Equal("This account is restricted. Contact the bank.", blocked.Errors[0]);
            }

            await WithAsync(s => s.UnrestrictAsync(kit.User.Id));
            using var again = _app.NewScope();
            var allowed = await again.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 10m });
            Assert.True(allowed.IsSuccess);
        }
    }
}
