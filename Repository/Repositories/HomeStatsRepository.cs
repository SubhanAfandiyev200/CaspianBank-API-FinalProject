using Domain.Constants;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class HomeStatsRepository : IHomeStatsRepository
    {
        private readonly AppDbContext _context;

        public HomeStatsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountCustomersAsync()
        {
            // İşçilər sayılmır: yalnız Customer rolu olan hesablar
            var customers = from userRole in _context.UserRoles
                            join role in _context.Roles on userRole.RoleId equals role.Id
                            where role.Name == Roles.Customer
                            select userRole.UserId;

            return await customers.CountAsync();
        }

        public async Task<int> CountTransfersAsync()
        {
            return await _context.Transactions
                .AsNoTracking()
                .CountAsync(t => t.Type == TransactionType.TransferOut);
        }

        public async Task<decimal> SumTransferAmountAsync()
        {
            // Sətir yoxdursa SumAsync null qaytarır, onu 0-a çeviririk
            var total = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Type == TransactionType.TransferOut)
                .SumAsync(t => (decimal?)t.Amount);

            return total ?? 0m;
        }

        public async Task<int> CountCardsAsync()
        {
            return await _context.Cards
                .AsNoTracking()
                .CountAsync();
        }
    }
}
