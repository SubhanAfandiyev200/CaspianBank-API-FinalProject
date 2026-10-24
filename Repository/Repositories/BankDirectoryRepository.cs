using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class BankDirectoryRepository : IBankDirectoryRepository
    {
        private readonly AppDbContext _dbContext;

        public BankDirectoryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ExternalBank?> FindExternalBankByBinAsync(string bin)
        {
            return await _dbContext.ExternalBanks.AsNoTracking().FirstOrDefaultAsync(b => b.Bin.StartsWith(bin));
        }

        public async Task<ExternalCard?> FindExternalCardAsync(string cardNumber)
        {
            return await _dbContext.ExternalCards
                .AsNoTracking()
                .Include(c => c.ExternalBank)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.IsActive);
        }

        public async Task<bool> AnyCaspianCardStartsWithAsync(string bin)
        {
            return await _dbContext.Cards.AsNoTracking().AnyAsync(c => c.CardNumber.StartsWith(bin));
        }
    }
}
