using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class CardRepository : BaseRepository<Card>, ICardRepository
    {
        public CardRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Card>> GetByUserAsync(string userId)
        {
            return await _dbSet
                    .AsNoTracking()
                    .Include(c => c.CardDesign)
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                    .ToListAsync();
        }

        public async Task<Card?> GetByIdForUserAsync(int cardId, string userId)
        {
            return await _dbSet
                    .Include(c => c.CardDesign)
                    .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        }

        public async Task<bool> TopUpAsync(Card card, decimal amount, string description)
        {
            card.Balance += amount;
            _dbContext.Transactions.Add(new Transaction
            {
                CardId = card.Id,
                Type = TransactionType.TopUp,
                IsIncome = true,
                Amount = amount,
                BalanceAfter = card.Balance,
                Description = description,
                Reference = "TU-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()
            });

            try
            {
                await _dbContext.SaveChangesAsync();   // balans və jurnal qeydi birlikdə yazılır
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<Card?> GetByNumberAsync(string cardNumber)
        {
            return await _dbSet
                    .Include(c => c.CardDesign)
                    .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<bool> CardNumberExistsAsync(string cardNumber)
        {
            return await _dbSet.AnyAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<bool> UserHasCardsAsync(string userId)
        {
            return await _dbSet.AnyAsync(c => c.UserId == userId);
        }

        public async Task<bool> AddRangeAsync(IEnumerable<Card> cards)
        {
            await _dbSet.AddRangeAsync(cards);
            try
            {
                await _dbContext.SaveChangesAsync();   // tək SaveChanges: ya hamısı yazılır, ya heç biri
                return true;
            }
            catch (DbUpdateException)
            {
                // Unikal index pozulub: iki sorğu eyni anda ilk kartları yaratmağa çalışıb
                _dbContext.ChangeTracker.Clear();
                return false;
            }
        }

        public async Task<CardOpenResult> AddPaidCardAsync(Card newCard, int fundingCardId, string userId, decimal fee)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            // Balans bu sorğuda yenidən oxunur (servisdən gələn rəqəmə güvənmirik)
            var funding = await _dbSet.FirstOrDefaultAsync(c => c.Id == fundingCardId && c.UserId == userId);
            if (funding is null)
            {
                return CardOpenResult.FundingCardNotFound;
            }
            if (funding.IsBlocked)
            {
                return CardOpenResult.FundingCardBlocked;
            }
            if (fee > 0 && funding.Balance < fee)
            {
                return CardOpenResult.InsufficientFunds;
            }

            funding.Balance -= fee;
            await _dbSet.AddAsync(newCard);

            if (fee > 0)
            {
                _dbContext.Transactions.Add(new Transaction
                {
                    CardId = funding.Id,
                    Type = TransactionType.CardFee,
                    IsIncome = false,
                    Amount = fee,
                    BalanceAfter = funding.Balance,
                    Description = $"{newCard.Tier} card opening fee",
                    Reference = "FE-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()
                });
            }

            try
            {
                // RowVersion: bu arada balans başqa əməliyyatla dəyişibsə DbUpdateConcurrencyException atılır
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return CardOpenResult.Success;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                _dbContext.ChangeTracker.Clear();
                return CardOpenResult.Conflict;
            }
        }
    }
}
