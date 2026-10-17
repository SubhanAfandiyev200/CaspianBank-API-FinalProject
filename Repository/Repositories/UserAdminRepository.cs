using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class UserAdminRepository : IUserAdminRepository
    {
        private readonly AppDbContext _dbContext;

        public UserAdminRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Customer rolunda olanlar
        private IQueryable<AppUser> Customers()
        {
            return from u in _dbContext.Users
                   join ur in _dbContext.UserRoles on u.Id equals ur.UserId
                   join r in _dbContext.Roles on ur.RoleId equals r.Id
                   where r.Name == Roles.Customer
                   select u;
        }

        public async Task<UserPageResult> GetCustomersPageAsync(string? search, bool? restricted, int skip, int take)
        {
            var all = Customers().AsNoTracking();
            var restrictedCount = await all.CountAsync(u => u.IsRestricted);

            var query = all;
            if (restricted.HasValue)
            {
                query = query.Where(u => u.IsRestricted == restricted.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = "%" + search.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
                query = query.Where(u => EF.Functions.Like(u.Name + " " + u.Surname, pattern)
                                      || EF.Functions.Like(u.Email!, pattern)
                                      || EF.Functions.Like(u.PhoneNumber!, pattern)
                                      || EF.Functions.Like(u.FinKod!, pattern));
            }

            var total = await query.CountAsync();
            var rows = await query
                .OrderBy(u => u.Name).ThenBy(u => u.Surname).ThenBy(u => u.Id)
                .Skip(skip)
                .Take(take)
                .Select(u => new UserRow
                {
                    User = u,
                    CardCount = u.Cards.Count(),
                    TotalBalance = u.Cards.Sum(c => (decimal?)c.Balance) ?? 0m
                })
                .ToListAsync();

            return new UserPageResult { Items = rows, TotalCount = total, RestrictedCount = restrictedCount };
        }

        public async Task<UserDetailData?> GetCustomerDetailAsync(string userId)
        {
            var user = await Customers().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return null;
            }

            var cards = await _dbContext.Cards.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Tier).ThenBy(c => c.Id)
                .ToListAsync();
            var cardIds = cards.Select(c => c.Id).ToList();

            var loans = await _dbContext.Loans.AsNoTracking()
                .Where(l => l.BorrowerUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var recent = await _dbContext.Transactions.AsNoTracking()
                .Where(t => cardIds.Contains(t.CardId))
                .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
                .Take(10)
                .ToListAsync();

            var sent = await _dbContext.Transactions.AsNoTracking()
                .Where(t => cardIds.Contains(t.CardId) && t.Type == TransactionType.TransferOut)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var bills = await _dbContext.BillPayments.AsNoTracking()
                .Where(p => p.UserId == userId)
                .GroupBy(p => 1)
                .Select(g => new { Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .FirstOrDefaultAsync();

            return new UserDetailData
            {
                User = user,
                Cards = cards,
                Loans = loans,
                RecentTransactions = recent,
                TotalSent = sent,
                BillPaymentCount = bills?.Count ?? 0,
                BillPaymentTotal = bills?.Total ?? 0m
            };
        }

        public async Task<AppUser?> GetCustomerAsync(string userId)
        {
            return await Customers().FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task SaveAsync(AppUser user)
        {
            // Yalnız bu istifadəçi yazılır
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
