using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // Qara siyahıdakı müştərinin krediti iki təsdiq tələb edir: əvvəl kredit şöbəsi, sonra Super Admin. İkisi olmadan pul getmir
    public class BlacklistedLoanApprovalTests
    {
        private static async Task<T> ReviewAsync<T>(TestApp app, Func<ILoanReviewService, Task<T>> action)
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ILoanReviewService>());
        }

        private static async Task<(CustomerKit Kit, LoanSummaryDto Loan)> BlacklistedApplicationAsync(TestApp app, decimal amount = 6000m)
        {
            var kit = await app.NewBorrowerAsync(blacklisted: true);
            var loan = await app.ApplyAsync(kit, amount, 12);
            return (kit, loan);
        }

        [Fact]
        public async Task The_loan_staff_approval_moves_it_to_the_super_admin_and_pays_nothing()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var superAdmin = await app.NewStaffAsync(Roles.SuperAdmin);
            var accountant = await app.NewStaffAsync(Roles.Accountant);

            var result = await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("AwaitingSuperAdmin", result.Data!.Status);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));

            var stored = await app.GetLoanAsync(loan.Id);
            Assert.Equal(accountant.Id, stored.FirstApprovedByUserId);
            Assert.Equal(6000m, stored.FirstApprovedAmount);
            Assert.Null(stored.ApprovedAmount);

            // Super Admin-ə sorğu gedir, müştəriyə heç nə deyilmir
            var ask = Assert.Single(await app.NotificationsAsync(superAdmin.Id), n => n.Title == "Loan for a blacklisted customer needs your approval");
            Assert.Contains("Do you approve it too?", ask.Message);
            Assert.DoesNotContain(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Your loan was approved");
        }

        [Fact]
        public async Task The_customer_only_sees_under_review_while_the_super_admin_decides()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            using var scope = app.NewScope();
            var mine = (await scope.ServiceProvider.GetRequiredService<ILoanService>().GetMyLoansAsync(kit.User.Id)).Single(l => l.Id == loan.Id);
            Assert.Equal("Pending", mine.Status);

            var staffView = await ReviewAsync(app, s => s.GetDetailAsync(loan.Id));
            Assert.Equal("AwaitingSuperAdmin", staffView.Data!.Loan.Status);
            Assert.Equal("FinalApproval", staffView.Data.Stage);
            Assert.True(staffView.Data.RequiresSuperAdminApproval);
            Assert.Equal(accountant.Name + " " + accountant.Surname, staffView.Data.FirstApprovedBy);
        }

        [Fact]
        public async Task The_super_admin_final_approval_pays_the_money()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var superAdmin = await app.NewStaffAsync(Roles.SuperAdmin);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            // Super Admin başqa məbləğ göndərsə də kredit şöbəsinin təsdiq etdiyi məbləğ keçir
            var result = await ReviewAsync(app, s => s.ApproveAsync(superAdmin.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 1000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Active", result.Data!.Status);
            Assert.Equal(6000m, await app.BalanceAsync(kit.Standard.Id));
            var stored = await app.GetLoanAsync(loan.Id);
            Assert.Equal(superAdmin.Id, stored.ReviewedByUserId);
            Assert.Equal(accountant.Id, stored.FirstApprovedByUserId);
            Assert.Contains(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Your loan was approved");
        }

        [Fact]
        public async Task A_reduced_amount_becomes_an_offer_after_the_super_admin_approves()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var superAdmin = await app.NewStaffAsync(Roles.SuperAdmin);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 4000m }));

            var result = await ReviewAsync(app, s => s.ApproveAsync(superAdmin.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 4000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Offered", result.Data!.Status);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_super_admin_alone_cannot_give_the_first_approval()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var superAdmin = await app.NewStaffAsync(Roles.SuperAdmin);

            var result = await ReviewAsync(app, s => s.ApproveAsync(superAdmin.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.False(result.IsSuccess);
            Assert.StartsWith("A loan for a customer on the blacklist must first be approved by the loan staff", result.Errors[0]);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal("Pending", (await app.GetLoanAsync(loan.Id)).Status.ToString());
        }

        [Fact]
        public async Task The_loan_staff_cannot_give_the_final_approval()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            var admin = await app.NewStaffAsync(Roles.Admin);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            var again = await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));
            var byAdmin = await ReviewAsync(app, s => s.ApproveAsync(admin.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.False(again.IsSuccess);
            Assert.False(byAdmin.IsSuccess);
            Assert.Equal("Only the Super Admin can give the final approval for a customer on the blacklist.", byAdmin.Errors[0]);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_super_admin_can_reject_and_the_customer_gets_the_general_message()
        {
            var app = await TestApp.SharedAsync();
            var (kit, loan) = await BlacklistedApplicationAsync(app);
            var superAdmin = await app.NewStaffAsync(Roles.SuperAdmin);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            var result = await ReviewAsync(app, s => s.RejectAsync(superAdmin.Id, loan.Id, new RejectLoanDto { Reason = "Too risky" }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Rejected", result.Data!.Status);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Contains(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Application not approved");
        }

        [Fact]
        public async Task The_loan_staff_cannot_reject_after_it_went_to_the_super_admin()
        {
            var app = await TestApp.SharedAsync();
            var (_, loan) = await BlacklistedApplicationAsync(app);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            var result = await ReviewAsync(app, s => s.RejectAsync(accountant.Id, loan.Id, new RejectLoanDto { Reason = "Changed my mind" }));

            Assert.False(result.IsSuccess);
            Assert.Equal("Only the Super Admin can decide on this application now.", result.Errors[0]);
        }

        [Fact]
        public async Task The_loan_staff_can_reject_a_blacklisted_application_at_the_first_step()
        {
            var app = await TestApp.SharedAsync();
            var (_, loan) = await BlacklistedApplicationAsync(app);
            var accountant = await app.NewStaffAsync(Roles.Accountant);

            var result = await ReviewAsync(app, s => s.RejectAsync(accountant.Id, loan.Id, new RejectLoanDto { Reason = "Blacklisted" }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Rejected", result.Data!.Status);
        }

        [Fact]
        public async Task An_ordinary_customer_still_needs_only_one_approval()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var loan = await app.ApplyAsync(kit, 6000m, 12);
            var accountant = await app.NewStaffAsync(Roles.Accountant);

            var result = await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Active", result.Data!.Status);
            Assert.Equal(6000m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_review_list_flags_the_blacklisted_applications_and_counts_the_ones_for_the_super_admin()
        {
            var app = await TestApp.SharedAsync();
            var (_, loan) = await BlacklistedApplicationAsync(app);
            var accountant = await app.NewStaffAsync(Roles.Accountant);
            await ReviewAsync(app, s => s.ApproveAsync(accountant.Id, loan.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            var page = await ReviewAsync(app, s => s.GetPageAsync(new LoanReviewFilterDto { Status = "AwaitingSuperAdmin", PageSize = 100 }));

            Assert.True(page.IsSuccess);
            Assert.True(page.Data!.AwaitingSuperAdminCount >= 1);
            var item = Assert.Single(page.Data.Items, i => i.Id == loan.Id);
            Assert.True(item.RequiresSuperAdmin);
            Assert.Equal("AwaitingSuperAdmin", item.Status);
        }
    }
}
