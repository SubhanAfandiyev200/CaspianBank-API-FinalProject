using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Loans;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class LoanServiceTests
    {
        private static string NewRequest()
        {
            return Guid.NewGuid().ToString();
        }

        private static PayLoanDto Pay(int cardId, string type = "Installment", decimal amount = 0m, string? request = null)
        {
            return new PayLoanDto { CardId = cardId, Type = type, Amount = amount, RequestId = request ?? NewRequest() };
        }

        private static async Task<T> WithLoanServiceAsync<T>(TestApp app, Func<ILoanService, Task<T>> action)
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ILoanService>());
        }

        private static async Task<T> WithReviewServiceAsync<T>(TestApp app, Func<ILoanReviewService, Task<T>> action)
        {
            using var scope = app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ILoanReviewService>());
        }

        // ---------- Qaydalar ----------

        [Fact]
        public async Task Options_show_the_bank_rules_and_let_a_normal_customer_apply()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();

            var options = await WithLoanServiceAsync(app, s => s.GetOptionsAsync(kit.User.Id));

            Assert.True(options.IsSuccess);
            Assert.Equal(500m, options.Data!.MinAmount);
            Assert.Equal(50000m, options.Data.MaxAmount);
            Assert.Equal(10m, options.Data.AnnualInterestRate);
            Assert.Equal(new[] { 6, 12, 18, 24, 36, 48 }, options.Data.TermOptions);
            Assert.Equal(3, options.Data.MaxActiveLoans);
            Assert.Equal(0, options.Data.OpenLoanCount);
            Assert.True(options.Data.CanApply);
            Assert.Null(options.Data.CannotApplyReason);
        }

        [Fact]
        public async Task Options_say_why_a_customer_cannot_apply()
        {
            var app = await TestApp.SharedAsync();
            var young = await app.NewBorrowerAsync(birthDate: DateTime.UtcNow.AddYears(-16));
            var restricted = await app.NewBorrowerAsync();
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == restricted.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            AppUser staff = await app.NewStaffAsync();

            var youngOptions = await WithLoanServiceAsync(app, s => s.GetOptionsAsync(young.User.Id));
            var restrictedOptions = await WithLoanServiceAsync(app, s => s.GetOptionsAsync(restricted.User.Id));
            var staffOptions = await WithLoanServiceAsync(app, s => s.GetOptionsAsync(staff.Id));
            var unknown = await WithLoanServiceAsync(app, s => s.GetOptionsAsync("nobody"));

            Assert.False(youngOptions.Data!.CanApply);
            Assert.Equal("You must be at least 18 years old to request a loan.", youngOptions.Data.CannotApplyReason);
            Assert.Equal("Your account is restricted. Please contact support.", restrictedOptions.Data!.CannotApplyReason);
            Assert.Contains("not linked to a citizen record", staffOptions.Data!.CannotApplyReason);
            Assert.Equal("Your account was not found.", unknown.Data!.CannotApplyReason);
        }

        // ---------- Müraciət ----------

        [Fact]
        public async Task Apply_creates_a_pending_application_with_the_contract_terms()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);

            var result = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id, 6000m, 12)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var summary = result.Data!;
            Assert.Equal("Pending", summary.Status);
            Assert.Equal(6000m, summary.RequestedAmount);
            Assert.Null(summary.ApprovedAmount);
            Assert.Equal(12, summary.TermMonths);
            Assert.Equal(10m, summary.AnnualInterestRate);
            Assert.Equal(LoanEngine.MonthlyPayment(6000m, 10m, 12), summary.MonthlyPayment);
            Assert.Matches(@"^KR-\d{4}-\d{6}$", summary.ContractNumber);

            var stored = await app.GetLoanAsync(summary.Id);
            Assert.Equal(kit.User.Id, stored.BorrowerUserId);
            Assert.Equal(3000m, stored.IncomeAtApply);
            Assert.Equal(10m, stored.InitialAnnualInterestRate);
            Assert.Equal(10m, stored.FixedPenalty);
            Assert.Equal(0.5m, stored.DailyPenaltyPercent);
            Assert.Equal(kit.Standard.Id, stored.TargetCardId);
            Assert.Empty(stored.Installments);                 // cədvəl yalnız pul veriləndə yaranır
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Apply_gives_each_application_the_next_contract_number()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();

            var first = await app.ApplyAsync(kit);
            var second = await app.ApplyAsync(kit);

            var firstNumber = int.Parse(first.ContractNumber[^6..]);
            var secondNumber = int.Parse(second.ContractNumber[^6..]);
            Assert.True(secondNumber > firstNumber);
            Assert.NotEqual(first.ContractNumber, second.ContractNumber);
        }

        [Fact]
        public async Task Parallel_applications_get_different_contract_numbers()
        {
            var app = await TestApp.SharedAsync();
            var kits = new List<CustomerKit>();
            for (var i = 0; i < 5; i++)
            {
                kits.Add(await app.NewBorrowerAsync());
            }

            var results = await Task.WhenAll(kits.Select(k => WithLoanServiceAsync(app, s => s.ApplyAsync(k.User.Id, LoanTestData.ApplyModel(k.Standard.Id)))));

            Assert.All(results, r => Assert.True(r.IsSuccess, string.Join(";", r.Errors)));
            Assert.Equal(5, results.Select(r => r.Data!.ContractNumber).Distinct().Count());
        }

        [Fact]
        public async Task Apply_notifies_the_money_staff()
        {
            var app = await TestApp.SharedAsync();
            var accountant = await app.NewStaffAsync(Domain.Constants.Roles.Accountant);
            var support = await app.NewStaffAsync(Domain.Constants.Roles.CustomerSupport);
            var kit = await app.NewBorrowerAsync();

            var summary = await app.ApplyAsync(kit, 6000m, 12);

            var notice = (await app.NotificationsAsync(accountant.Id)).Single(n => n.Title == "New loan application" && n.LinkUrl == $"/Admin/Loans/Detail/{summary.Id}");
            Assert.Equal(NotificationCategory.Staff, notice.Category);
            Assert.Contains("6,000 AZN over 12 months", notice.Message);
            Assert.DoesNotContain(await app.NotificationsAsync(support.Id), n => n.Title == "New loan application");
        }

        [Fact]
        public async Task Apply_rejects_amounts_outside_the_bank_range()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();

            var low = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id, 499m)));
            var high = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id, 50001m)));

            Assert.Equal("The amount must be between 500 and 50,000 AZN.", low.Errors[0]);
            Assert.Equal("The amount must be between 500 and 50,000 AZN.", high.Errors[0]);
        }

        [Fact]
        public async Task Apply_accepts_the_range_limits_and_rejects_unavailable_terms()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();

            var minimum = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id, 500m, 6)));
            var wrongTerm = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id, 1000m, 7)));

            Assert.True(minimum.IsSuccess);
            Assert.Equal("Choose one of the available terms.", wrongTerm.Errors[0]);
        }

        [Fact]
        public async Task Apply_requires_a_current_job()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(salary: 0m);

            var result = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id)));

            Assert.False(result.IsSuccess);
            Assert.Equal("We couldn't find a current job in your record, so a loan can't be requested.", result.Errors[0]);
        }

        [Fact]
        public async Task Apply_by_a_blacklisted_citizen_waits_for_the_two_approvals()
        {
            var app = await TestApp.SharedAsync();
            var accountant = await app.NewStaffAsync();
            var kit = await app.NewBorrowerAsync(blacklisted: true);

            var result = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id)));

            Assert.True(result.IsSuccess);
            Assert.Equal("Pending", result.Data!.Status);
            var stored = await app.GetLoanAsync(result.Data.Id);
            Assert.True(stored.RequiresSuperAdminApproval);
            Assert.Null(stored.ReviewedAt);
            Assert.Contains(await app.NotificationsAsync(accountant.Id), n => n.LinkUrl == $"/Admin/Loans/Detail/{result.Data.Id}" && n.Title.Contains("blacklisted"));
        }

        [Fact]
        public async Task A_blacklisted_citizen_can_apply_even_without_a_job()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(salary: 0m, blacklisted: true);

            var result = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id)));

            Assert.True(result.IsSuccess);
            Assert.Equal("Pending", result.Data!.Status);
        }

        [Fact]
        public async Task Apply_checks_the_card_that_receives_the_money()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var other = await app.NewBorrowerAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m, blocked: true);

            var cashback = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Cashback.Id)));
            var foreign = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(other.Standard.Id)));
            var blockedResult = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(blocked.Id)));

            Assert.Equal("The money can't be paid to a Cashback card. Choose another card.", cashback.Errors[0]);
            Assert.Equal("Choose one of your own cards to receive the money.", foreign.Errors[0]);
            Assert.Equal("That card is blocked. Choose another card.", blockedResult.Errors[0]);
        }

        [Fact]
        public async Task Apply_is_limited_to_the_maximum_number_of_open_loans()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            for (var i = 0; i < 3; i++)
            {
                await app.ApplyAsync(kit);
            }

            var fourth = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, LoanTestData.ApplyModel(kit.Standard.Id)));

            Assert.False(fourth.IsSuccess);
            Assert.Equal("You already have 3 open loans or applications. The limit is 3.", fourth.Errors[0]);
        }

        [Fact]
        public async Task Apply_is_refused_for_underage_and_restricted_customers()
        {
            var app = await TestApp.SharedAsync();
            var young = await app.NewBorrowerAsync(birthDate: DateTime.UtcNow.AddYears(-17));
            var restricted = await app.NewBorrowerAsync();
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == restricted.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });

            var youngResult = await WithLoanServiceAsync(app, s => s.ApplyAsync(young.User.Id, LoanTestData.ApplyModel(young.Standard.Id)));
            var restrictedResult = await WithLoanServiceAsync(app, s => s.ApplyAsync(restricted.User.Id, LoanTestData.ApplyModel(restricted.Standard.Id)));

            Assert.Equal("You must be at least 18 years old to request a loan.", youngResult.Errors[0]);
            Assert.Equal("Your account is restricted. Please contact support.", restrictedResult.Errors[0]);
        }

        [Fact]
        public async Task Apply_reports_validation_errors()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();

            var result = await WithLoanServiceAsync(app, s => s.ApplyAsync(kit.User.Id, new ApplyLoanDto { Amount = 0m, Purpose = "Nope", TermMonths = 0, TargetCardId = 0 }));

            Assert.False(result.IsSuccess);
            Assert.Contains("Enter the amount you need.", result.Errors);
            Assert.Contains("Choose the purpose of the loan.", result.Errors);
        }

        // ---------- Qərar ----------

        [Fact]
        public async Task Approving_the_full_amount_pays_the_loan_out_to_the_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m, 100m);
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("Active", result.Data!.Status);
            Assert.Equal(6000m, result.Data.ApprovedAmount);
            Assert.Equal(6100m, await app.BalanceAsync(kit.Standard.Id));

            var loan = await app.GetLoanAsync(applied.Id);
            Assert.Equal(12, loan.Installments.Count);
            Assert.Equal(6000m, loan.Installments.Sum(i => i.PrincipalAmount));
            Assert.Equal(6000m, loan.OutstandingPrincipal);
            Assert.Equal(staff.Id, loan.ReviewedByUserId);
            Assert.NotNull(loan.DisbursedAt);

            var row = Assert.Single(await app.TransactionsAsync(kit.Standard.Id));
            Assert.Equal(TransactionType.LoanDisbursement, row.Type);
            Assert.Equal(6000m, row.Amount);
            Assert.Equal("LD-" + applied.ContractNumber, row.Reference);
        }

        [Fact]
        public async Task Approval_tells_the_customer_and_writes_the_treasury_entry()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Your loan was approved");
            Assert.Contains("6,000.00 AZN", notice.Message);
            Assert.Equal($"/App/Loans/{applied.Id}", notice.LinkUrl);

            var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == "LD-" + applied.ContractNumber).ToListAsync());
            var entry = Assert.Single(entries);
            Assert.Equal(TreasuryEntryKind.LoanPaidOut, entry.Kind);
            Assert.Equal(-6000m, entry.Amount);
        }

        [Fact]
        public async Task Approving_less_makes_an_offer_that_the_customer_must_accept()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m, 0m);
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 4000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Offered", result.Data!.Status);
            Assert.Equal(4000m, result.Data.ApprovedAmount);
            Assert.Equal(LoanEngine.MonthlyPayment(4000m, 10m, 12), result.Data.MonthlyPayment);
            Assert.InRange((result.Data.OfferExpiresAt!.Value - DateTime.UtcNow).TotalDays, 6.9, 7.1);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));          // pul hələ düşməyib
            Assert.Empty((await app.GetLoanAsync(applied.Id)).Installments);
            var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "We have an offer for you");
            Assert.Contains("4,000.00 AZN instead of 6,000.00 AZN", notice.Message);
        }

        [Fact]
        public async Task Approval_cannot_exceed_the_request_or_leave_the_bank_range()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            var above = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000.01m }));
            var below = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 499m }));
            var zero = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 0m }));

            Assert.Equal("The approved amount can't be higher than the requested amount.", above.Errors[0]);
            Assert.Equal("The approved amount must be between 500 and 50,000 AZN.", below.Errors[0]);
            Assert.False(zero.IsSuccess);
            Assert.Equal("Pending", (await app.GetLoanAsync(applied.Id)).Status.ToString());
        }

        [Fact]
        public async Task An_application_can_be_decided_only_once()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            var first = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));
            var second = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));
            var reject = await WithReviewServiceAsync(app, s => s.RejectAsync(staff.Id, applied.Id, new RejectLoanDto { Reason = "late attempt" }));

            Assert.True(first.IsSuccess);
            Assert.Equal("This application has already been reviewed.", second.Errors[0]);
            Assert.Equal("This application has already been reviewed.", reject.Errors[0]);
            Assert.Equal(6000m, await app.BalanceAsync(kit.Standard.Id));       // yalnız bir dəfə
        }

        [Fact]
        public async Task Parallel_approvals_pay_the_loan_out_once()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            var staff = await app.NewStaffAsync();

            var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }))));

            Assert.Equal(1, results.Count(r => r.IsSuccess));
            Assert.Equal(6000m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(12, (await app.GetLoanAsync(applied.Id)).Installments.Count);
        }

        [Fact]
        public async Task Approval_of_an_unknown_application_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var staff = await app.NewStaffAsync();
            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, 999999, new ApproveLoanDto { ApprovedAmount = 1000m }));
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task A_citizen_blacklisted_after_applying_needs_the_super_admin_too()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            await app.QueryAsync(async db =>
            {
                await db.Customers.Where(c => c.Id == kit.Customer.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlacklisted, true));
                return 0;
            });
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.True(result.IsSuccess);
            Assert.Equal("AwaitingSuperAdmin", result.Data!.Status);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Approval_is_refused_when_the_customer_already_has_the_maximum_active_loans()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(10000m);
            for (var i = 0; i < 3; i++)
            {
                await app.ActiveLoanAsync(kit, 600m, 6);
            }
            // açıq limit dolduğu üçün yeni müraciət yaratmaq olmur: bir aktiv krediti müvəqqəti bağlayıb müraciət yaradırıq
            var active = await app.QueryAsync(db => db.Loans.AsNoTracking().Where(l => l.BorrowerUserId == kit.User.Id).Select(l => l.Id).FirstAsync());
            await app.QueryAsync(async db =>
            {
                await db.Loans.Where(l => l.Id == active).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LoanStatus.Closed));
                return 0;
            });
            var applied = await app.ApplyAsync(kit, 600m, 6);
            await app.QueryAsync(async db =>
            {
                await db.Loans.Where(l => l.Id == active).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LoanStatus.Active));
                return 0;
            });
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 600m }));

            Assert.False(result.IsSuccess);
            Assert.Equal("The customer already has 3 active loans. The limit is 3.", result.Errors[0]);
        }

        [Fact]
        public async Task Approval_is_refused_when_the_target_card_was_blocked_meanwhile()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, 6000m, 12);
            await app.QueryAsync(async db =>
            {
                await db.Cards.Where(c => c.Id == kit.Standard.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlocked, true));
                return 0;
            });
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

            Assert.False(result.IsSuccess);
            Assert.Contains("can't receive the money", result.Errors[0]);
            Assert.Equal("Pending", (await app.GetLoanAsync(applied.Id)).Status.ToString());
        }

        [Fact]
        public async Task Approval_is_refused_when_the_treasury_cannot_cover_the_loan_and_the_reserve()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync();
                var applied = await app.ApplyAsync(kit, 6000m, 12);
                // xəzinədə 5000 qalır, kredit 6000
                await app.QueryAsync(async db =>
                {
                    db.TreasuryEntries.Add(new TreasuryEntry { Kind = TreasuryEntryKind.OperatingCost, Amount = -(2400000m - 5000m), Description = "test", Reference = "T-LOW" });
                    await db.SaveChangesAsync();
                    return 0;
                });
                var staff = await app.NewStaffAsync();

                var result = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = 6000m }));

                Assert.False(result.IsSuccess);
                Assert.Equal("The bank can't pay out this loan right now. Please try again later.", result.Errors[0]);
                Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
                Assert.Equal("Pending", (await app.GetLoanAsync(applied.Id)).Status.ToString());
            }
        }

        [Fact]
        public async Task Reject_closes_the_application_and_tells_the_customer_a_general_message()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit);
            var staff = await app.NewStaffAsync();

            var result = await WithReviewServiceAsync(app, s => s.RejectAsync(staff.Id, applied.Id, new RejectLoanDto { Reason = "  Income too low  " }));

            Assert.True(result.IsSuccess);
            Assert.Equal("Rejected", result.Data!.Status);
            var stored = await app.GetLoanAsync(applied.Id);
            Assert.Equal("Income too low", stored.RejectReason);
            Assert.Equal(staff.Id, stored.ReviewedByUserId);
            var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Application not approved");
            Assert.DoesNotContain("Income", notice.Message);       // daxili səbəb müştəriyə göstərilmir
        }

        [Fact]
        public async Task Reject_needs_a_reason_and_a_pending_application()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit);
            var staff = await app.NewStaffAsync();

            var noReason = await WithReviewServiceAsync(app, s => s.RejectAsync(staff.Id, applied.Id, new RejectLoanDto { Reason = "" }));
            var unknown = await WithReviewServiceAsync(app, s => s.RejectAsync(staff.Id, 999999, new RejectLoanDto { Reason = "nope" }));

            Assert.False(noReason.IsSuccess);
            Assert.True(unknown.IsNotFound);
        }

        // ---------- Təklif ----------

        private static async Task<(CustomerKit Kit, LoanSummaryDto Offer)> OfferAsync(TestApp app, decimal asked = 6000m, decimal offered = 4000m)
        {
            var kit = await app.NewBorrowerAsync();
            var applied = await app.ApplyAsync(kit, asked, 12);
            var staff = await app.NewStaffAsync();
            var offer = await WithReviewServiceAsync(app, s => s.ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = offered }));
            Assert.Equal("Offered", offer.Data!.Status);
            return (kit, offer.Data);
        }

        [Fact]
        public async Task Accepting_the_offer_pays_the_reduced_amount_and_builds_the_schedule()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);

            var result = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto()));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("Active", result.Data!.Status);
            Assert.Equal(4000m, await app.BalanceAsync(kit.Standard.Id));
            var loan = await app.GetLoanAsync(offer.Id);
            Assert.Equal(12, loan.Installments.Count);
            Assert.Equal(4000m, loan.Installments.Sum(i => i.PrincipalAmount));
            Assert.NotNull(loan.AcceptedAt);
        }

        [Fact]
        public async Task The_offer_money_can_go_to_another_own_card()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            var result = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto { TargetCardId = silver.Id }));

            Assert.True(result.IsSuccess);
            Assert.Equal(4000m, await app.BalanceAsync(silver.Id));
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_offer_money_cannot_go_to_somebody_elses_card()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);
            var stranger = await app.NewBorrowerAsync();

            var result = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto { TargetCardId = stranger.Standard.Id }));

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose one of your own cards.", result.Errors[0]);
            Assert.Equal(0m, await app.BalanceAsync(stranger.Standard.Id));
        }

        [Fact]
        public async Task An_expired_offer_cannot_be_accepted_and_is_declined()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);
            await app.QueryAsync(async db =>
            {
                await db.Loans.Where(l => l.Id == offer.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.OfferExpiresAt, DateTime.UtcNow.AddMinutes(-5)));
                return 0;
            });

            var result = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto()));

            Assert.False(result.IsSuccess);
            Assert.Equal("This offer has expired.", result.Errors[0]);
            Assert.Equal("Declined", (await app.GetLoanAsync(offer.Id)).Status.ToString());
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Declining_the_offer_closes_it_without_money()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);

            var result = await WithLoanServiceAsync(app, s => s.DeclineOfferAsync(kit.User.Id, offer.Id));

            Assert.True(result.IsSuccess);
            Assert.Equal("Declined", result.Data!.Status);
            Assert.Equal(0m, await app.BalanceAsync(kit.Standard.Id));
            var again = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto()));
            Assert.Equal("This offer is no longer open.", again.Errors[0]);
        }

        [Fact]
        public async Task Nobody_else_can_answer_my_offer()
        {
            var app = await TestApp.SharedAsync();
            var (_, offer) = await OfferAsync(app);
            var stranger = await app.NewBorrowerAsync();

            var accept = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(stranger.User.Id, offer.Id, new AcceptLoanOfferDto()));
            var decline = await WithLoanServiceAsync(app, s => s.DeclineOfferAsync(stranger.User.Id, offer.Id));

            Assert.True(accept.IsNotFound);
            Assert.True(decline.IsNotFound);
        }

        [Fact]
        public async Task A_restricted_customer_cannot_accept_an_offer()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });

            var result = await WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto()));

            Assert.Equal("Your account is restricted. Please contact support.", result.Errors[0]);
        }

        [Fact]
        public async Task Parallel_accepts_pay_the_offer_once()
        {
            var app = await TestApp.SharedAsync();
            var (kit, offer) = await OfferAsync(app);

            var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                WithLoanServiceAsync(app, s => s.AcceptOfferAsync(kit.User.Id, offer.Id, new AcceptLoanOfferDto()))));

            Assert.Equal(1, results.Count(r => r.IsSuccess));
            Assert.Equal(4000m, await app.BalanceAsync(kit.Standard.Id));
        }

        // ---------- Mənim kreditlərim ----------

        [Fact]
        public async Task My_loans_list_and_detail_show_only_my_loans()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewBorrowerAsync();
            var other = await app.NewBorrowerAsync();
            var loan = await app.ActiveLoanAsync(mine, 6000m, 12);
            await app.ApplyAsync(other);

            var list = (await WithLoanServiceAsync(app, s => s.GetMyLoansAsync(mine.User.Id))).ToList();
            var detail = await WithLoanServiceAsync(app, s => s.GetMyLoanAsync(mine.User.Id, loan.Id));
            var foreign = await WithLoanServiceAsync(app, s => s.GetMyLoanAsync(other.User.Id, loan.Id));

            Assert.Single(list);
            Assert.Equal(loan.Id, list[0].Id);
            Assert.NotNull(list[0].NextDueDate);
            Assert.True(detail.IsSuccess);
            Assert.Equal(12, detail.Data!.Installments.Count);
            Assert.True(detail.Data.CanPayInstallment);
            Assert.True(foreign.IsNotFound);
        }

        // ---------- Ödəniş ----------

        private static decimal FirstInstallment(Loan loan)
        {
            var first = loan.Installments.OrderBy(i => i.Number).First();
            return first.PrincipalAmount + first.InterestAmount;
        }

        [Fact]
        public async Task Paying_the_next_installment_debits_the_card_and_records_the_split()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var before = await app.GetLoanAsync(loan.Id);
            var payment = FirstInstallment(before);
            var first = before.Installments.OrderBy(i => i.Number).First();

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal(payment, result.Data!.Amount);
            Assert.Equal(first.InterestAmount, result.Data.Interest);
            Assert.Equal(first.PrincipalAmount, result.Data.Principal);
            Assert.Equal(0m, result.Data.Penalty);
            Assert.Equal("Installment", result.Data.Type);
            Assert.False(result.Data.LoanClosed);
            Assert.StartsWith("LP-", result.Data.Reference);
            Assert.Equal(6000m - payment, await app.BalanceAsync(kit.Standard.Id));

            var after = await app.GetLoanAsync(loan.Id);
            var paid = after.Installments.OrderBy(i => i.Number).First();
            Assert.Equal(payment, paid.PaidAmount);
            Assert.NotNull(paid.FullyPaidAt);
            Assert.Equal(6000m - first.PrincipalAmount, after.OutstandingPrincipal);
            Assert.Single(after.PaymentRecords);
        }

        [Fact]
        public async Task The_payment_is_written_to_the_card_journal_and_the_treasury()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            var row = (await app.TransactionsAsync(kit.Standard.Id)).Single(t => t.Type == TransactionType.LoanPayment);
            Assert.Equal(result.Data!.Amount, row.Amount);
            Assert.Equal(result.Data.Reference, row.Reference);
            Assert.Equal($"Payment on loan {loan.ContractNumber}", row.Description);

            var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == result.Data.Reference).ToListAsync());
            Assert.Equal(result.Data.Principal, entries.Single(e => e.Kind == TreasuryEntryKind.LoanPrincipal).Amount);
            Assert.Equal(result.Data.Interest, entries.Single(e => e.Kind == TreasuryEntryKind.LoanInterest).Amount);
        }

        [Fact]
        public async Task A_part_payment_covers_interest_first()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "Installment", 10m)));

            Assert.True(result.IsSuccess);
            Assert.Equal(10m, result.Data!.Amount);
            Assert.Equal(10m, result.Data.Interest);
            Assert.Equal(0m, result.Data.Principal);
            Assert.Equal(6000m - 10m, await app.BalanceAsync(kit.Standard.Id));
            var after = await app.GetLoanAsync(loan.Id);
            Assert.Null(after.Installments.OrderBy(i => i.Number).First().FullyPaidAt);
        }

        [Fact]
        public async Task Several_part_payments_add_up_to_a_paid_installment()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var payment = FirstInstallment(await app.GetLoanAsync(loan.Id));

            await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "Installment", 100m)));
            var rest = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "Installment", payment - 100m)));

            Assert.True(rest.IsSuccess, string.Join(";", rest.Errors));
            var paid = (await app.GetLoanAsync(loan.Id)).Installments.OrderBy(i => i.Number).First();
            Assert.NotNull(paid.FullyPaidAt);
            Assert.Equal(payment, paid.PaidAmount);
        }

        [Fact]
        public async Task An_amount_above_what_is_due_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var payment = FirstInstallment(await app.GetLoanAsync(loan.Id));

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "Installment", payment + 0.01m)));

            Assert.False(result.IsSuccess);
            Assert.StartsWith("That is more than what is due now", result.Errors[0]);
            Assert.Equal(6000m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Payment_fails_when_the_card_balance_is_too_low()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            await app.SetBalanceAsync(kit.Standard.Id, 1m);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.False(result.IsSuccess);
            Assert.Equal("That card doesn't have enough balance for this payment.", result.Errors[0]);
            Assert.Equal(1m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Payment_cannot_use_a_blocked_or_somebody_elses_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 1000m, blocked: true);
            var stranger = await app.NewKitAsync(1000m);

            var blockedResult = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(blocked.Id)));
            var foreignResult = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(stranger.Standard.Id)));

            Assert.Equal("That card is blocked. Choose another card.", blockedResult.Errors[0]);
            Assert.Equal("Choose one of your own cards to pay from.", foreignResult.Errors[0]);
            Assert.Equal(1000m, await app.BalanceAsync(stranger.Standard.Id));
        }

        [Fact]
        public async Task The_same_request_id_does_not_charge_twice()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var request = NewRequest();

            var first = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, request: request)));
            var again = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, request: request)));

            Assert.True(first.IsSuccess);
            Assert.True(again.IsSuccess, string.Join(";", again.Errors));
            Assert.Equal(first.Data!.Reference, again.Data!.Reference);
            Assert.Equal(first.Data.Amount, again.Data.Amount);
            Assert.Equal(6000m - first.Data.Amount, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Parallel_payments_with_one_request_id_charge_once()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var request = NewRequest();

            var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, request: request)))));

            Assert.All(results, r => Assert.True(r.IsSuccess, string.Join(";", r.Errors)));
            var paid = FirstInstallment(await app.GetLoanAsync(loan.Id));
            Assert.Equal(6000m - paid, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task A_late_installment_adds_the_fixed_penalty_and_the_daily_percent()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            await app.SetFirstDueDateAsync(loan.Id, today.AddDays(-10));
            var before = await app.GetLoanAsync(loan.Id);
            var unpaid = FirstInstallment(before);
            var expectedPenalty = 10m + LoanEngine.Round(unpaid * 0.5m / 100m * 10);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal(expectedPenalty, result.Data!.Penalty);
            var second = before.Installments.OrderBy(i => i.Number).Skip(1).First();
            Assert.Equal(unpaid + expectedPenalty + second.PrincipalAmount + second.InterestAmount, result.Data.Amount);      // gecikmiş ödəniş + növbəti ödəniş
            var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == result.Data.Reference).ToListAsync());
            Assert.Equal(expectedPenalty, entries.Single(e => e.Kind == TreasuryEntryKind.LoanPenalty).Amount);
        }

        [Fact]
        public async Task A_part_payment_on_a_late_installment_pays_the_penalty_first()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            await app.SetFirstDueDateAsync(loan.Id, today.AddDays(-10));

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "Installment", 12m)));

            Assert.True(result.IsSuccess);
            Assert.Equal(12m, result.Data!.Penalty);          // cərimə 10 + gündəlik hissə > 12: bütün 12 cəriməyə gedir
            Assert.Equal(0m, result.Data.Interest);
            Assert.Equal(0m, result.Data.Principal);
        }

        [Fact]
        public async Task A_loan_can_be_paid_by_someone_else_with_its_code_and_the_owner_is_told()
        {
            var app = await TestApp.SharedAsync();
            var owner = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(owner, 6000m, 12);
            var helper = await app.NewKitAsync(5000m);
            var payment = FirstInstallment(await app.GetLoanAsync(loan.Id));

            var result = await WithLoanServiceAsync(app, s => s.PayByCodeAsync(helper.User.Id, new PayByCodeDto { ContractNumber = loan.ContractNumber.ToLowerInvariant(), CardId = helper.Standard.Id, Amount = 0m, RequestId = NewRequest() }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal(payment, result.Data!.Amount);
            Assert.Equal(5000m - payment, await app.BalanceAsync(helper.Standard.Id));
            Assert.Equal(6000m, await app.BalanceAsync(owner.Standard.Id));
            var notice = Assert.Single(await app.NotificationsAsync(owner.User.Id), n => n.Title == "Payment received");
            Assert.Contains("from another account", notice.Message);
            Assert.Contains(loan.ContractNumber, notice.Message);
        }

        [Fact]
        public async Task Paying_by_code_can_be_a_part_amount()
        {
            var app = await TestApp.SharedAsync();
            var owner = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(owner, 6000m, 12);
            var helper = await app.NewKitAsync(100m);

            var result = await WithLoanServiceAsync(app, s => s.PayByCodeAsync(helper.User.Id, new PayByCodeDto { ContractNumber = loan.ContractNumber, CardId = helper.Standard.Id, Amount = 4m, RequestId = NewRequest() }));

            Assert.True(result.IsSuccess);
            Assert.Equal(4m, result.Data!.Amount);
            Assert.Equal(96m, await app.BalanceAsync(helper.Standard.Id));
        }

        [Fact]
        public async Task Paying_by_an_unknown_code_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var helper = await app.NewKitAsync(100m);

            var result = await WithLoanServiceAsync(app, s => s.PayByCodeAsync(helper.User.Id, new PayByCodeDto { ContractNumber = "KR-2026-999999", CardId = helper.Standard.Id, RequestId = NewRequest() }));

            Assert.Equal("We couldn't find a loan with this code.", result.Errors[0]);
        }

        [Fact]
        public async Task Early_payments_belong_to_the_borrower_only()
        {
            var app = await TestApp.SharedAsync();
            var owner = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(owner, 6000m, 12);
            var stranger = await app.NewKitAsync(10000m);

            var partial = await WithLoanServiceAsync(app, s => s.PayAsync(stranger.User.Id, loan.Id, Pay(stranger.Standard.Id, "PartialEarly", 500m)));
            var full = await WithLoanServiceAsync(app, s => s.PayAsync(stranger.User.Id, loan.Id, Pay(stranger.Standard.Id, "FullEarly")));

            Assert.Equal("Only the borrower can make this payment.", partial.Errors[0]);
            Assert.Equal("Only the borrower can make this payment.", full.Errors[0]);
            Assert.Equal(10000m, await app.BalanceAsync(stranger.Standard.Id));
        }

        [Fact]
        public async Task A_partial_early_payment_lowers_the_balance_the_rate_and_the_monthly_payment()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var paymentBefore = (await app.GetLoanAsync(loan.Id)).MonthlyPayment;

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "PartialEarly", 1000m)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("PartialEarly", result.Data!.Type);
            Assert.Equal(9m, result.Data.NewInterestRate);          // 1000 >= 10% x 6000: faiz 1 bənd düşür
            Assert.Equal(5000m, await app.BalanceAsync(kit.Standard.Id));
            var after = await app.GetLoanAsync(loan.Id);
            Assert.Equal(9m, after.AnnualInterestRate);
            Assert.Equal(5000m, after.OutstandingPrincipal);
            Assert.True(after.MonthlyPayment < paymentBefore);
            Assert.Equal(12, after.Installments.Count(i => !i.IsCancelled));
        }

        [Fact]
        public async Task A_partial_early_payment_below_the_minimum_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "PartialEarly", 5m)));

            Assert.Equal("The minimum early payment is 10.00 AZN.", result.Errors[0]);
        }

        [Fact]
        public async Task Paying_off_closes_the_loan_cancels_the_future_installments_and_tells_the_owner()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            await app.SetBalanceAsync(kit.Standard.Id, 20000m);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id, "FullEarly")));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.True(result.Data!.LoanClosed);
            Assert.Equal("FullEarly", result.Data.Type);
            Assert.Equal(20000m - result.Data.Amount, await app.BalanceAsync(kit.Standard.Id));
            var after = await app.GetLoanAsync(loan.Id);
            Assert.Equal(LoanStatus.Closed, after.Status);
            Assert.True(after.ClosedEarly);
            Assert.All(after.Installments, i => Assert.True(i.IsCancelled));
            Assert.Equal(0m, after.OutstandingPrincipal);
            Assert.Contains(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Loan paid off" && n.Message.Contains(loan.ContractNumber));

            var again = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));
            Assert.Equal("This loan is not open for payments.", again.Errors[0]);
        }

        [Fact]
        public async Task Paying_every_installment_closes_the_loan_normally()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 600m, 6);
            await app.SetBalanceAsync(kit.Standard.Id, 10000m);

            for (var i = 0; i < 6; i++)
            {
                var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));
                Assert.True(result.IsSuccess, $"payment {i + 1}: " + string.Join(";", result.Errors));
            }

            var after = await app.GetLoanAsync(loan.Id);
            Assert.Equal(LoanStatus.Closed, after.Status);
            Assert.False(after.ClosedEarly);
            Assert.Equal(0m, after.OutstandingPrincipal);
            Assert.Equal(6, after.PaymentRecords.Count);
        }

        [Fact]
        public async Task Nothing_is_due_after_the_last_installment()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 600m, 6);
            await app.SetBalanceAsync(kit.Standard.Id, 10000m);
            for (var i = 0; i < 6; i++)
            {
                await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));
            }

            var extra = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.False(extra.IsSuccess);
        }

        [Fact]
        public async Task A_restricted_customer_cannot_pay_and_an_unknown_loan_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);

            var unknownLoan = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, 999999, Pay(kit.Standard.Id)));
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var restricted = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.True(unknownLoan.IsNotFound);
            Assert.Equal("Your account is restricted. Please contact support.", restricted.Errors[0]);
        }

        [Fact]
        public async Task A_loan_in_court_can_still_be_paid()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 600m, 6);
            await app.QueryAsync(async db =>
            {
                await db.Loans.Where(l => l.Id == loan.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LoanStatus.Court));
                return 0;
            });
            await app.SetBalanceAsync(kit.Standard.Id, 5000m);

            var result = await WithLoanServiceAsync(app, s => s.PayAsync(kit.User.Id, loan.Id, Pay(kit.Standard.Id)));

            Assert.True(result.IsSuccess);
        }

        // ---------- Ödəniş kodu ilə axtarış ----------

        [Fact]
        public async Task Lookup_shows_the_masked_owner_and_what_is_due()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            var payment = FirstInstallment(await app.GetLoanAsync(loan.Id));

            var result = await WithLoanServiceAsync(app, s => s.LookupAsync("  " + loan.ContractNumber.ToLowerInvariant() + " "));

            Assert.True(result.IsSuccess);
            Assert.Equal(loan.ContractNumber, result.Data!.ContractNumber);
            Assert.Equal("Borrower T.", result.Data.OwnerName);
            Assert.Equal(payment, result.Data.DueTotal);
            Assert.Equal(0m, result.Data.OverdueTotal);
            Assert.True(result.Data.CanPay);
            Assert.NotNull(result.Data.NextDueDate);
        }

        [Fact]
        public async Task Lookup_hides_loans_that_are_not_open_and_rejects_a_bad_format()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync();
            var pending = await app.ApplyAsync(kit);

            var notOpen = await WithLoanServiceAsync(app, s => s.LookupAsync(pending.ContractNumber));
            var badFormat = await WithLoanServiceAsync(app, s => s.LookupAsync("12345"));
            var empty = await WithLoanServiceAsync(app, s => s.LookupAsync(""));
            var unknown = await WithLoanServiceAsync(app, s => s.LookupAsync("KR-2026-999999"));

            Assert.True(notOpen.IsNotFound);
            Assert.Equal("Enter the loan code like KR-2026-000123.", badFormat.Errors[0]);
            Assert.False(empty.IsSuccess);
            Assert.True(unknown.IsNotFound);
        }

        [Fact]
        public async Task Lookup_of_a_late_loan_shows_the_overdue_amount()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewBorrowerAsync(3000m);
            var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
            await app.SetFirstDueDateAsync(loan.Id, LoanEngine.BakuToday(DateTime.UtcNow).AddDays(-5));

            var result = await WithLoanServiceAsync(app, s => s.LookupAsync(loan.ContractNumber));

            Assert.True(result.Data!.OverdueTotal > 0m);
            Assert.True(result.Data.DueTotal >= result.Data.OverdueTotal);
        }

        // ---------- Gündəlik iş ----------

        [Fact]
        public async Task Maintenance_declines_expired_offers_and_tells_the_customer_once()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var (kit, offer) = await OfferAsync(app);
                await app.QueryAsync(async db =>
                {
                    await db.Loans.Where(l => l.Id == offer.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.OfferExpiresAt, DateTime.UtcNow.AddHours(-1)));
                    return 0;
                });

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());
                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                Assert.Equal("Declined", (await app.GetLoanAsync(offer.Id)).Status.ToString());
                Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Offer expired");
            }
        }

        [Fact]
        public async Task Maintenance_reminds_about_an_installment_due_within_three_days_only_once()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync(3000m);
                var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
                await app.SetFirstDueDateAsync(loan.Id, LoanEngine.BakuToday(DateTime.UtcNow).AddDays(2));

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());
                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Payment due soon");
                Assert.Contains(loan.ContractNumber, notice.Message);
            }
        }

        [Fact]
        public async Task Maintenance_does_not_remind_about_installments_that_are_far_away_or_paid()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync(3000m);
                var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
                await app.SetFirstDueDateAsync(loan.Id, LoanEngine.BakuToday(DateTime.UtcNow).AddDays(10));

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                Assert.DoesNotContain(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Payment due soon");
            }
        }

        [Fact]
        public async Task Maintenance_accrues_the_penalty_and_warns_about_a_late_installment()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync(3000m);
                var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
                await app.SetFirstDueDateAsync(loan.Id, LoanEngine.BakuToday(DateTime.UtcNow).AddDays(-4));

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                var first = (await app.GetLoanAsync(loan.Id)).Installments.OrderBy(i => i.Number).First();
                Assert.Equal(10m, first.FixedPenaltyAmount);
                Assert.True(first.AccruedPenalty > 0m);
                var notice = Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Payment overdue");
                Assert.Contains("penalty grows every day", notice.Message);
            }
        }

        [Fact]
        public async Task Maintenance_moves_a_loan_that_is_ninety_days_late_to_court_once()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync(3000m);
                var loan = await app.ActiveLoanAsync(kit, 6000m, 12);
                await app.SetFirstDueDateAsync(loan.Id, LoanEngine.BakuToday(DateTime.UtcNow).AddDays(-95));

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());
                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                var after = await app.GetLoanAsync(loan.Id);
                Assert.Equal(LoanStatus.Court, after.Status);
                Assert.NotNull(after.CourtAt);
                Assert.Single(await app.NotificationsAsync(kit.User.Id), n => n.Title == "Loan passed to court");
            }
        }

        [Fact]
        public async Task Maintenance_leaves_a_healthy_loan_alone()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewBorrowerAsync(3000m);
                var loan = await app.ActiveLoanAsync(kit, 6000m, 12);

                await WithLoanServiceAsync(app, s => s.RunDailyMaintenanceAsync());

                var after = await app.GetLoanAsync(loan.Id);
                Assert.Equal(LoanStatus.Active, after.Status);
                Assert.All(after.Installments, i => Assert.Equal(0m, i.AccruedPenalty));
                Assert.Empty((await app.NotificationsAsync(kit.User.Id)).Where(n => n.Category == NotificationCategory.Loan && n.Title != "Your loan was approved"));
            }
        }
    }
}
