using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.Loans;
using Service.Services.Interfaces;

namespace CaspianBank.Tests.Infrastructure
{
    // Kredit testləri üçün köməkçilər: işləyən müştəri, müraciət, təsdiq
    public static class LoanTestData
    {
        // Hazırda işləyən müştəri (maaşla) + Standard və Cashback kart
        public static async Task<CustomerKit> NewBorrowerAsync(this TestApp app, decimal salary = 3000m, decimal standardBalance = 0m,
                                                               DateTime? birthDate = null, bool blacklisted = false)
        {
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.EnsureOwnBankAsync(db);

            var customer = await TestData.NewCustomerAsync(db, "Borrower", "Test", birthDate ?? new DateTime(1990, 1, 1), blacklisted: blacklisted);
            if (blacklisted)
            {
                customer.BlacklistReason = "Unpaid loans";
                await db.SaveChangesAsync();
            }

            var workplace = new Workplace { Name = "Test LLC " + TestData.Next(), TaxId = "TX" + TestData.Next(), Sector = "IT", Address = "Baku" };
            db.Workplaces.Add(workplace);
            await db.SaveChangesAsync();
            if (salary > 0m)
            {
                db.Employments.Add(new Employment { CustomerId = customer.Id, WorkplaceId = workplace.Id, Position = "Engineer", MonthlySalary = salary, StartDate = new DateTime(2020, 1, 1) });
                await db.SaveChangesAsync();
            }

            var user = await TestData.NewUserAsync(scope.ServiceProvider, customer);
            var standard = await TestData.NewCardAsync(db, user.Id, CardTier.Standard, standardBalance);
            var cashback = await TestData.NewCardAsync(db, user.Id, CardTier.Cashback, 0m);
            return new CustomerKit(customer, user, standard, cashback);
        }

        public static async Task<AppUser> NewStaffAsync(this TestApp app, string role = Roles.Accountant)
        {
            using var scope = app.NewScope();
            return await TestData.NewUserAsync(scope.ServiceProvider, null, role);
        }

        public static ApplyLoanDto ApplyModel(int cardId, decimal amount = 6000m, int months = 12)
        {
            return new ApplyLoanDto { Amount = amount, Purpose = "Consumer", TermMonths = months, TargetCardId = cardId };
        }

        public static async Task<LoanSummaryDto> ApplyAsync(this TestApp app, CustomerKit kit, decimal amount = 6000m, int months = 12)
        {
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ILoanService>().ApplyAsync(kit.User.Id, ApplyModel(kit.Standard.Id, amount, months));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            return result.Data!;
        }

        // Müraciət + tam təsdiq: kredit aktivdir, pul karta düşüb
        public static async Task<LoanSummaryDto> ActiveLoanAsync(this TestApp app, CustomerKit kit, decimal amount = 6000m, int months = 12)
        {
            var applied = await app.ApplyAsync(kit, amount, months);
            var staff = await app.NewStaffAsync();
            using var scope = app.NewScope();
            var approved = await scope.ServiceProvider.GetRequiredService<ILoanReviewService>().ApproveAsync(staff.Id, applied.Id, new ApproveLoanDto { ApprovedAmount = amount });
            Assert.True(approved.IsSuccess, string.Join(";", approved.Errors));
            Assert.Equal("Active", approved.Data!.Status);
            return approved.Data!;
        }

        // Bütün aylıq ödənişlərin tarixini geri çəkir (gecikmə yaratmaq üçün): days gün əvvələ
        public static async Task ShiftDueDatesAsync(this TestApp app, int loanId, int days)
        {
            await app.QueryAsync(async db =>
            {
                var installments = await db.LoanInstallments.Where(i => i.LoanId == loanId).ToListAsync();
                foreach (var installment in installments)
                {
                    installment.DueDate = installment.DueDate.AddDays(-days);
                }
                await db.SaveChangesAsync();
                return 0;
            });
        }

        // Birinci aylıq ödənişin tarixini dəqiq təyin edir (qalanlar ondan sonra aylıq)
        public static async Task SetFirstDueDateAsync(this TestApp app, int loanId, DateTime firstDue)
        {
            await app.QueryAsync(async db =>
            {
                var installments = await db.LoanInstallments.Where(i => i.LoanId == loanId).OrderBy(i => i.Number).ToListAsync();
                for (var i = 0; i < installments.Count; i++)
                {
                    installments[i].DueDate = Domain.Loans.LoanEngine.LastDayOfMonth(firstDue.AddMonths(i));
                }
                installments[0].DueDate = firstDue;
                await db.SaveChangesAsync();
                return 0;
            });
        }

        public static async Task<Loan> GetLoanAsync(this TestApp app, int loanId)
        {
            return await app.QueryAsync(db => db.Loans.AsNoTracking().Include(l => l.Installments).Include(l => l.PaymentRecords).FirstAsync(l => l.Id == loanId));
        }

        private static class Assert
        {
            public static void True(bool condition, string message)
            {
                if (!condition)
                {
                    throw new InvalidOperationException("Test setup failed: " + message);
                }
            }

            public static void Equal(string expected, string actual)
            {
                if (expected != actual)
                {
                    throw new InvalidOperationException($"Test setup failed: expected {expected} but was {actual}");
                }
            }
        }
    }
}
