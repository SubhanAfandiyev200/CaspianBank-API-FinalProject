using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        // Öz kartları arasında köçürmənin təsviri bu sözlə başlayır (TransferService yazır): xərc sayılmır
        private const string OwnTransferPrefix = "Transfer to your";

        private readonly AppDbContext _dbContext;

        public AnalyticsRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<OutgoingRow>> GetOutgoingAsync(string userId, DateTime fromUtc)
        {
            var types = new[]
            {
                TransactionType.TransferOut, TransactionType.BillPayment, TransactionType.LoanPayment,
                TransactionType.Commission, TransactionType.CardFee, TransactionType.DepositOpen
            };

            return await _dbContext.Transactions.AsNoTracking()
                .Where(t => t.Card.UserId == userId && !t.IsIncome && t.CreatedAt >= fromUtc
                            && types.Contains(t.Type) && !t.Description.StartsWith(OwnTransferPrefix))
                .Select(t => new OutgoingRow(t.Type, t.Amount, t.CreatedAt))
                .ToListAsync();
        }
    }
}
