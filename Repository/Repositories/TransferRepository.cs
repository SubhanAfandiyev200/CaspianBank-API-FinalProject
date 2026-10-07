using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class TransferRepository : ITransferRepository
    {
        private const int MaxAttempts = 8;

        private readonly AppDbContext _dbContext;

        public TransferRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TransferResult> ExecuteAsync(TransferPlan plan)
        {
            // Eyni anda başqa əməliyyat bu kartların balansını dəyişibsə (RowVersion) bir neçə dəfə təzədən cəhd edilir
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Balanslar bu tranzaksiyada təzədən oxunur: servisdən gələn rəqəmlərə güvənmirik
                var source = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == plan.FromCardId && c.UserId == plan.UserId);
                var destination = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == plan.ToCardId);

                if (source is null)
                {
                    return new TransferResult(TransferOutcome.SourceNotFound);
                }
                if (destination is null)
                {
                    return new TransferResult(TransferOutcome.DestinationNotFound);
                }
                if (source.IsBlocked)
                {
                    return new TransferResult(TransferOutcome.SourceBlocked);
                }
                if (destination.IsBlocked)
                {
                    return new TransferResult(TransferOutcome.DestinationBlocked);
                }

                // Cashback kartına heç bir köçürmə olmur; Cashback kartından yalnız öz kartlarına köçürmək olar
                var isOwn = destination.UserId == plan.UserId;
                if (destination.Tier == CardTier.Cashback || (!isOwn && source.Tier == CardTier.Cashback))
                {
                    return new TransferResult(TransferOutcome.NotAllowed);
                }

                if (source.Balance < plan.Amount + plan.Commission)
                {
                    return new TransferResult(TransferOutcome.InsufficientFunds);
                }

                source.Balance -= plan.Amount;
                _dbContext.Transactions.Add(NewRow(source.Id, TransactionType.TransferOut, false, plan.Amount, source.Balance,
                    plan.DescriptionOut, plan.Reference, plan.Note));

                if (plan.Commission > 0)
                {
                    source.Balance -= plan.Commission;
                    _dbContext.Transactions.Add(NewRow(source.Id, TransactionType.Commission, false, plan.Commission, source.Balance,
                        "Transfer commission", plan.Reference, null));
                }

                destination.Balance += plan.Amount;
                _dbContext.Transactions.Add(NewRow(destination.Id, TransactionType.TransferIn, true, plan.Amount, destination.Balance,
                    plan.DescriptionIn, plan.Reference, plan.Note));

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new TransferResult(TransferOutcome.Success, source.Balance);
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();   // növbəti cəhd yenidən oxuyub təkrar yoxlayır
                    await Task.Delay(Random.Shared.Next(10, 60));   // eyni anda gələn sorğular növbə ilə keçsin
                }
                catch (DbUpdateException)
                {
                    // (Reference, Type) unikal indeksi: bu köçürmə artıq yazılıb
                    await transaction.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    return new TransferResult(TransferOutcome.Duplicate);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return new TransferResult(TransferOutcome.Conflict);
        }

        public async Task<TransferRecord?> FindAsync(string reference, string userId)
        {
            var rows = await _dbContext.Transactions
                .AsNoTracking()
                .Include(t => t.Card)
                .Where(t => t.Reference == reference)
                .ToListAsync();

            // Yalnız göndərənin öz köçürməsi qaytarılır (başqasının Reference-i ilə məlumat çıxmasın)
            var outRow = rows.FirstOrDefault(r => r.Type == TransactionType.TransferOut && r.Card.UserId == userId);
            var inRow = rows.FirstOrDefault(r => r.Type == TransactionType.TransferIn);
            if (outRow is null || inRow is null)
            {
                return null;
            }

            var fee = rows.FirstOrDefault(r => r.Type == TransactionType.Commission);
            return new TransferRecord(outRow.Card, inRow.Card, outRow.Amount, fee?.Amount ?? 0m, outRow.Note,
                fee?.BalanceAfter ?? outRow.BalanceAfter, outRow.CreatedAt);
        }

        private static Transaction NewRow(int cardId, TransactionType type, bool income, decimal amount, decimal balanceAfter,
                                          string description, string reference, string? note)
        {
            return new Transaction
            {
                CardId = cardId,
                Type = type,
                IsIncome = income,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Description = description,
                Reference = reference,
                Note = note
            };
        }
    }
}
