using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.Responses;
using Service.Helpers.Validators.Cards;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class CardService : ICardService
    {
        private const int ValidYears = 4;

        private readonly ICardRepository _cards;
        private readonly ICardTierConfigRepository _tiers;
        private readonly ITransactionRepository _transactions;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<CreateInitialCardsDto> _initialValidator;
        private readonly IValidator<AddCardDto> _addValidator;
        private readonly IValidator<BlockCardDto> _blockValidator;
        private readonly IValidator<TopUpDto> _topUpValidator;

        public CardService(ICardRepository cards,
                           ICardTierConfigRepository tiers,
                           ITransactionRepository transactions,
                           IAccountRepository accounts,
                           IValidator<CreateInitialCardsDto> initialValidator,
                           IValidator<AddCardDto> addValidator,
                           IValidator<BlockCardDto> blockValidator,
                           IValidator<TopUpDto> topUpValidator)
        {
            _cards = cards;
            _tiers = tiers;
            _transactions = transactions;
            _accounts = accounts;
            _initialValidator = initialValidator;
            _addValidator = addValidator;
            _blockValidator = blockValidator;
            _topUpValidator = topUpValidator;
        }

        public async Task<IEnumerable<CardDto>> GetMyCardsAsync(string userId)
        {
            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return Array.Empty<CardDto>();
            }

            var cards = await _cards.GetByUserAsync(userId);
            var holder = HolderName(user);
            return cards.Select(c => ToDto(c, holder, c.CardDesign.Image)).ToList();
        }

        public async Task<ServiceResult<CardDetailDto>> GetMyCardAsync(string userId, int cardId)
        {
            var user = await _accounts.GetByIdAsync(userId);
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (user is null || card is null)
            {
                return ServiceResult<CardDetailDto>.NotFound();
            }

            var config = await _tiers.GetByTierAsync(card.Tier);

            var dto = new CardDetailDto
            {
                CardNumber = card.CardNumber,
                IssueFee = config?.IssueFee ?? 0,
                CashbackPercent = config?.CashbackPercent ?? 0,
                TransferLimit = config?.TransferLimit ?? 0,
                CommissionPercent = config?.CommissionPercent ?? 0
            };
            Fill(dto, card, HolderName(user), card.CardDesign.Image);
            return ServiceResult<CardDetailDto>.Ok(dto);
        }

        public async Task<ServiceResult<IEnumerable<CardDto>>> CreateInitialCardsAsync(string userId, CreateInitialCardsDto model)
        {
            var validation = await _initialValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<IEnumerable<CardDto>>.NotFound();
            }

            if (await _cards.UserHasCardsAsync(userId))
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Your first cards have already been created.");
            }

            var fin = model.Fin.Trim().ToUpperInvariant();
            if (await _accounts.FinInUseAsync(fin, userId))
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("This FIN code is already registered to another account.");
            }

            var standard = await _tiers.GetByTierAsync(CardTier.Standard);
            var cashback = await _tiers.GetByTierAsync(CardTier.Cashback);
            if (standard is null || cashback is null)
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Card settings are not available right now. Please try again later.");
            }

            // FİN hesaba yazılır (bazada unikaldır)
            if (user.FinKod != fin)
            {
                user.FinKod = fin;
                var update = await _accounts.UpdateAsync(user);
                if (!update.Succeeded)
                {
                    return ServiceResult<IEnumerable<CardDto>>.Fail("This FIN code could not be saved. Check it and try again.");
                }
            }

            var created = new List<Card>
            {
                await NewCardAsync(userId, standard, 0m),
                await NewCardAsync(userId, cashback, 0m)
            };

            if (!await _cards.AddRangeAsync(created))
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Your first cards have already been created.");
            }

            var holder = HolderName(user);
            var result = created.Select(c => ToDto(c, holder, c.Tier == CardTier.Cashback ? cashback.CardDesign.Image : standard.CardDesign.Image)).ToList();
            return ServiceResult<IEnumerable<CardDto>>.Ok(result);
        }

        public async Task<ServiceResult<CardDto>> AddCardAsync(string userId, AddCardDto model)
        {
            var validation = await _addValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<CardDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<CardDto>.NotFound();
            }

            var tier = Enum.Parse<CardTier>(model.Tier, true);
            var config = await _tiers.GetByTierAsync(tier);
            if (config is null)
            {
                return ServiceResult<CardDto>.Fail("This card type is not available right now.");
            }

            var newCard = await NewCardAsync(userId, config, 0m);

            // Haqqın çıxılması və kartın yaranması bir tranzaksiyadadır
            var outcome = await _cards.AddPaidCardAsync(newCard, model.FundingCardId, userId, config.IssueFee);
            switch (outcome)
            {
                case CardOpenResult.Success:
                    return ServiceResult<CardDto>.Ok(ToDto(newCard, HolderName(user), config.CardDesign.Image));
                case CardOpenResult.FundingCardNotFound:
                    return ServiceResult<CardDto>.Fail("Choose one of your own cards to charge.");
                case CardOpenResult.FundingCardBlocked:
                    return ServiceResult<CardDto>.Fail("That card is blocked. Choose another card.");
                case CardOpenResult.InsufficientFunds:
                    return ServiceResult<CardDto>.Fail("That card doesn't have enough balance for the opening fee.");
                default:
                    return ServiceResult<CardDto>.Fail("The balance changed while the card was being opened. Please try again.");
            }
        }

        public async Task<ServiceResult<CardDto>> TopUpAsync(string userId, int cardId, TopUpDto model)
        {
            var validation = await _topUpValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<CardDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (user is null || card is null)
            {
                return ServiceResult<CardDto>.NotFound();
            }

            if (card.IsBlocked)
            {
                return ServiceResult<CardDto>.Fail("This card is blocked.");
            }

            // Cashback kartı yalnız cashback yığır: ona xaricdən pul yatırılmır
            if (card.Tier == CardTier.Cashback)
            {
                return ServiceResult<CardDto>.Fail("The Cashback card can't be topped up. Choose another card.");
            }

            // Xarici kartın nömrəsi/CVV-si yoxlanılır, amma saxlanmır və log-a yazılmır (simulyasiya).
            // Jurnala yalnız kartın son 4 rəqəmi yazılır.
            var digits = TopUpDtoValidator.Digits(model.SourceCardNumber);
            var description = $"Top up from card •••• {digits[^4..]}";

            if (!await _cards.TopUpAsync(card, decimal.Round(model.Amount, 2), description))
            {
                return ServiceResult<CardDto>.Fail("The balance changed while the top-up was processed. Please try again.");
            }

            return ServiceResult<CardDto>.Ok(ToDto(card, HolderName(user), card.CardDesign.Image));
        }

        public async Task<ServiceResult<IEnumerable<TransactionDto>>> GetTransactionsAsync(string userId, int cardId, int take)
        {
            // Kart yalnız sahibinə aiddirsə əməliyyatlar qaytarılır
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (card is null)
            {
                return ServiceResult<IEnumerable<TransactionDto>>.NotFound();
            }

            var limit = Math.Clamp(take, 1, 100);
            var rows = await _transactions.GetByCardAsync(cardId, limit);

            var result = rows.Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                IsIncome = t.IsIncome,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                Note = t.Note,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt
            }).ToList();

            return ServiceResult<IEnumerable<TransactionDto>>.Ok(result);
        }

        public async Task<IEnumerable<TransactionDto>> GetRecentActivityAsync(string userId, int take)
        {
            var limit = Math.Clamp(take, 1, 50);
            var rows = await _transactions.GetRecentForUserAsync(userId, limit);

            return rows.Select(t => new TransactionDto
            {
                Id = t.Id,
                CardId = t.CardId,
                CardLabel = t.Card.Tier + " •••• " + t.Card.CardNumber[^4..],
                Type = t.Type.ToString(),
                IsIncome = t.IsIncome,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                Note = t.Note,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<ServiceResult<CardDto>> BlockAsync(string userId, int cardId, BlockCardDto model)
        {
            var validation = await _blockValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<CardDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (user is null || card is null)
            {
                return ServiceResult<CardDto>.NotFound();
            }

            if (card.IsBlocked)
            {
                return ServiceResult<CardDto>.Fail("This card is already blocked.");
            }

            if (await _accounts.IsLockedOutAsync(user))
            {
                return ServiceResult<CardDto>.Fail("Too many failed attempts. Try again in a few minutes.");
            }

            if (!string.Equals(model.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<CardDto>.Fail("Enter the email on this account.");
            }

            // Parol yanlışdırsa giriş cəhdləri kimi sayılır (5 uğursuz cəhddən sonra hesab müvəqqəti bloklanır)
            if (!await _accounts.CheckPasswordAsync(user, model.Password))
            {
                await _accounts.RegisterFailedAttemptAsync(user);
                return ServiceResult<CardDto>.Fail("Password doesn't match.");
            }

            await _accounts.ResetFailedAttemptsAsync(user);

            card.IsBlocked = true;
            await _cards.UpdateAsync(card);

            return ServiceResult<CardDto>.Ok(ToDto(card, HolderName(user), card.CardDesign.Image));
        }

        // ---- köməkçilər ----

        private async Task<Card> NewCardAsync(string userId, CardTierConfig config, decimal balance)
        {
            // Nömrə təkrarlanmasın (unikal index də var): bir neçə dəfə cəhd edilir
            string number;
            var attempts = 0;
            do
            {
                number = CardNumberGenerator.Generate();
                attempts++;
            }
            while (await _cards.CardNumberExistsAsync(number) && attempts < 10);

            var now = DateTime.UtcNow;
            var expiryYear = now.Year + ValidYears;

            return new Card
            {
                UserId = userId,
                Tier = config.Tier,
                CardDesignId = config.CardDesignId,
                CardNumber = number,
                ExpiryDate = new DateTime(expiryYear, now.Month, DateTime.DaysInMonth(expiryYear, now.Month), 0, 0, 0, DateTimeKind.Utc),
                Balance = balance,
                IsBlocked = false
            };
        }


        private static string HolderName(AppUser user)
        {
            return $"{user.Name} {user.Surname}".Trim().ToUpperInvariant();
        }

        private static CardDto ToDto(Card card, string holder, string designImage)
        {
            var dto = new CardDto();
            Fill(dto, card, holder, designImage);
            return dto;
        }

        private static void Fill(CardDto dto, Card card, string holder, string designImage)
        {
            dto.Id = card.Id;
            dto.Tier = card.Tier.ToString();
            dto.Last4 = card.CardNumber[^4..];
            dto.Expiry = card.ExpiryDate.ToString("MM/yy", CultureInfo.InvariantCulture);
            dto.Balance = card.Balance;
            dto.IsBlocked = card.IsBlocked;
            dto.HolderName = holder;
            dto.DesignImage = designImage;
        }
    }
}
