using Domain.Constants;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class BankDashboardRepository : IBankDashboardRepository
    {
        private readonly AppDbContext _dbContext;

        public BankDashboardRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MonthMoney> GetMonthMoneyAsync(DateTime fromUtc, DateTime toUtc)
        {
            var transactions = _dbContext.Transactions.AsNoTracking().Where(t => t.CreatedAt >= fromUtc && t.CreatedAt < toUtc);

            var commissions = await transactions.Where(t => t.Type == TransactionType.Commission).SumAsync(t => (decimal?)t.Amount);
            var cardFees = await transactions.Where(t => t.Type == TransactionType.CardFee).SumAsync(t => (decimal?)t.Amount);
            var cashback = await transactions.Where(t => t.Type == TransactionType.Cashback).SumAsync(t => (decimal?)t.Amount);

            var payments = _dbContext.LoanPaymentRecords.AsNoTracking().Where(p => p.PaidAt >= fromUtc && p.PaidAt < toUtc);
            var interest = await payments.SumAsync(p => (decimal?)p.InterestPart);
            var penalties = await payments.SumAsync(p => (decimal?)p.PenaltyPart);

            return new MonthMoney
            {
                Commissions = commissions ?? 0m,
                CardFees = cardFees ?? 0m,
                LoanInterest = interest ?? 0m,
                Penalties = penalties ?? 0m,
                Cashback = cashback ?? 0m
            };
        }

        public async Task<BankSnapshot> GetSnapshotAsync(DateTime monthFromUtc, DateTime monthToUtc)
        {
            // İşçilər sayılmır: yalnız Customer rolu olan hesablar
            var customers = from u in _dbContext.Users.AsNoTracking()
                            join ur in _dbContext.UserRoles on u.Id equals ur.UserId
                            join r in _dbContext.Roles on ur.RoleId equals r.Id
                            where r.Name == Roles.Customer
                            select u;

            var totalUsers = await customers.CountAsync();
            var newUsers = await customers.CountAsync(u => u.CreatedAt >= monthFromUtc && u.CreatedAt < monthToUtc);
            var restricted = await customers.CountAsync(u => u.IsRestricted);

            var deposits = await _dbContext.Cards.AsNoTracking().SumAsync(c => (decimal?)c.Balance);
            var outstanding = await _dbContext.Loans.AsNoTracking()
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Court)
                .SumAsync(l => (decimal?)l.OutstandingPrincipal);
            var pendingLoans = await _dbContext.Loans.AsNoTracking().CountAsync(l => l.Status == LoanStatus.Pending);
            var pendingTickets = await _dbContext.SupportTickets.AsNoTracking().CountAsync(t => t.Status == TicketStatus.Pending);

            return new BankSnapshot
            {
                TotalUsers = totalUsers,
                NewUsers = newUsers,
                RestrictedUsers = restricted,
                CustomerDeposits = deposits ?? 0m,
                LoansOutstanding = outstanding ?? 0m,
                PendingLoans = pendingLoans,
                PendingTickets = pendingTickets
            };
        }
    }
}
