using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.DTOs.History;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HistoryService : IHistoryService
    {
        // Bazada vaxt UTC saxlanılır, istifadəçi isə Bakı vaxtı ilə düşünür (UTC+4, yay vaxtı yoxdur).
        // Seçilən "gün" Bakı günüdür: onu UTC-yə çevirmək üçün 4 saat çıxırıq
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        private readonly ITransactionRepository _transactions;
        private readonly ICardRepository _cards;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<HistoryFilterDto> _historyValidator;
        private readonly IValidator<StatementFilterDto> _statementValidator;

        public HistoryService(ITransactionRepository transactions,
                              ICardRepository cards,
                              IAccountRepository accounts,
                              IValidator<HistoryFilterDto> historyValidator,
                              IValidator<StatementFilterDto> statementValidator)
        {
            _transactions = transactions;
            _cards = cards;
            _accounts = accounts;
            _historyValidator = historyValidator;
            _statementValidator = statementValidator;
        }

        public async Task<HistoryPageDto> GetHistoryAsync(string userId, HistoryFilterDto filter)
        {
            await _historyValidator.EnsureValidAsync(filter);

            // Seçilən kart bu istifadəçiyə aid olmalıdır: başqasının kartının nömrəsini yazan 404 alır
            if (filter.CardId.HasValue)
            {
                var card = await _cards.GetByIdForUserAsync(filter.CardId.Value, userId);
                if (card is null) throw new NotFoundException();
            }

            TransactionType? type = null;
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                type = Enum.Parse<TransactionType>(filter.Type, true);
            }

            bool? isIncome = null;
            if (!string.IsNullOrWhiteSpace(filter.Direction))
            {
                isIncome = filter.Direction.Equals("in", StringComparison.OrdinalIgnoreCase);
            }

            var fromUtc = filter.From.HasValue ? ToUtcStart(filter.From.Value) : (DateTime?)null;
            var toUtc = filter.To.HasValue ? ToUtcStart(filter.To.Value.AddDays(1)) : (DateTime?)null;

            var page = filter.Page;
            var pageSize = filter.PageSize;
            var result = await _transactions.GetPageForUserAsync(userId, filter.CardId, type, isIncome, fromUtc, toUtc,
                                                                 filter.Search, (page - 1) * pageSize, pageSize);

            // Mövcud olmayan səhifə istənilibsə son səhifəyə çəkilir (boş siyahı göstərmək əvəzinə)
            var totalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize);
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
                result = await _transactions.GetPageForUserAsync(userId, filter.CardId, type, isIncome, fromUtc, toUtc,
                                                                 filter.Search, (page - 1) * pageSize, pageSize);
            }

            return new HistoryPageDto
            {
                Items = result.Items.Select(t => ToDto(t, true)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                TotalPages = totalPages,
                TotalIn = result.TotalIn,
                TotalOut = result.TotalOut
            };
        }

        public async Task<StatementDto> GetStatementAsync(string userId, StatementFilterDto filter)
        {
            await _statementValidator.EnsureValidAsync(filter);

            var card = await _cards.GetByIdForUserAsync(filter.CardId!.Value, userId);
            if (card is null) throw new NotFoundException();

            var fromUtc = ToUtcStart(filter.From!.Value);
            var toUtc = ToUtcStart(filter.To!.Value.AddDays(1));

            // Açılış balansı = aralıqdan əvvəlki son əməliyyatdan sonrakı balans (heç nə yoxdursa kart 0-dan başlayıb)
            var before = await _transactions.GetLastBeforeAsync(card.Id, fromUtc);
            var opening = before is null ? 0m : before.BalanceAfter;

            var rows = await _transactions.GetByCardInRangeAsync(card.Id, fromUtc, toUtc);

            // Bağlanış balansı = aralıqdakı son əməliyyatdan sonrakı balans (əməliyyat yoxdursa açılış balansı)
            var closing = rows.Count == 0 ? opening : rows[rows.Count - 1].BalanceAfter;

            var user = await _accounts.GetByIdAsync(userId);
            var holder = user is null ? string.Empty : (user.Name + " " + user.Surname).Trim().ToUpperInvariant();

            return new StatementDto
            {
                CardId = card.Id,
                CardLabel = card.Tier + " •••• " + card.CardNumber[^4..],
                HolderName = holder,
                From = filter.From.Value.Date,
                To = filter.To.Value.Date,
                OpeningBalance = opening,
                ClosingBalance = closing,
                TotalIn = rows.Where(t => t.IsIncome).Sum(t => t.Amount),
                TotalOut = rows.Where(t => !t.IsIncome).Sum(t => t.Amount),
                Rows = rows.Select(t => ToDto(t, false)).ToList()
            };
        }

        // Bakı günü (00:00) → UTC an: 4 saat əvvəl. Kind=Utc qoyulur ki, bazaya düzgün müqayisə getsin
        private static DateTime ToUtcStart(DateTime bakuDay)
        {
            return DateTime.SpecifyKind(bakuDay.Date - BakuOffset, DateTimeKind.Utc);
        }

        // withCard: kart məlumatı yüklənibsə (siyahıda) kartın etiketi də yazılır
        private static TransactionDto ToDto(Transaction t, bool withCard)
        {
            return new TransactionDto
            {
                Id = t.Id,
                CardId = t.CardId,
                CardLabel = withCard ? t.Card.Tier + " •••• " + t.Card.CardNumber[^4..] : string.Empty,
                Type = t.Type.ToString(),
                IsIncome = t.IsIncome,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                Note = t.Note,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
