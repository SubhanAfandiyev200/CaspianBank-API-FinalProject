using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
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
        private readonly IValidator<AddCardDto> _addValidator;
        private readonly IValidator<BlockCardDto> _blockValidator;
        private readonly IValidator<UnblockCardDto> _unblockValidator;
        private readonly IOtpService _otp;
        private readonly IValidator<TopUpDto> _topUpValidator;
        private readonly INotificationService _notifications;
        private readonly IBankDirectoryRepository _directory;
        private readonly ILogger<CardService> _logger;

        public CardService(ICardRepository cards,
                           ICardTierConfigRepository tiers,
                           ITransactionRepository transactions,
                           IAccountRepository accounts,
                           IValidator<AddCardDto> addValidator,
                           IValidator<BlockCardDto> blockValidator,
                           IValidator<UnblockCardDto> unblockValidator,
                           IOtpService otp,
                           IValidator<TopUpDto> topUpValidator,
                           INotificationService notifications,
                           IBankDirectoryRepository directory,
                           ILogger<CardService> logger)
        {
            _notifications = notifications;
            _directory = directory;
            _logger = logger;
            _cards = cards;
            _tiers = tiers;
            _transactions = transactions;
            _accounts = accounts;
            _addValidator = addValidator;
            _blockValidator = blockValidator;
            _unblockValidator = unblockValidator;
            _otp = otp;
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

        public async Task<ServiceResult<IEnumerable<CardDto>>> CreateInitialCardsAsync(string userId)
        {
            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<IEnumerable<CardDto>>.NotFound();
            }

            // Kartlar yalnız reyestrdəki vətəndaşa bağlı hesaba açılır (qeydiyyatda bağlanır)
            if (user.CustomerId is null)
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Your account is not linked to a citizen record, so cards can't be opened.");
            }

            if (await _cards.UserHasCardsAsync(userId))
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Your first cards have already been created.");
            }

            var standard = await _tiers.GetByTierAsync(CardTier.Standard);
            var cashback = await _tiers.GetByTierAsync(CardTier.Cashback);
            if (standard is null || cashback is null)
            {
                return ServiceResult<IEnumerable<CardDto>>.Fail("Card settings are not available right now. Please try again later.");
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

            // Pul yalnız həqiqi kartdan gəlir: ya Caspian kartından (həmin kartdan çıxır), ya da bazada olan başqa bank kartından (onun balansı bizdə yoxdur, ona görə çıxmır)
            var digits = TopUpDtoValidator.Digits(model.SourceCardNumber);
            var amount = decimal.Round(model.Amount, 2);
            var source = await _cards.GetByNumberAsync(digits);

            if (source is not null)
            {
                if (source.Id == card.Id)
                {
                    return ServiceResult<CardDto>.Fail("Choose a different card from the one you're topping up.");
                }

                // Bitmə tarixi kartdakı ilə eyni olmalıdır (CVV saxlanmır, ona görə yalnız formatı yoxlanır)
                if (!string.Equals(model.Expiry.Trim(), source.ExpiryDate.ToString("MM/yy", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                {
                    return ServiceResult<CardDto>.Fail("The card details are not correct.");
                }

                if (source.IsBlocked)
                {
                    return ServiceResult<CardDto>.Fail("That card is blocked. Choose another card.");
                }

                if (source.Balance < amount)
                {
                    return ServiceResult<CardDto>.Fail("That card doesn't have enough balance for this payment.");
                }

                if (!await _cards.TopUpFromCardAsync(source, card, amount))
                {
                    return ServiceResult<CardDto>.Fail("The balance changed while the top-up was processed. Please try again.");
                }

                _logger.LogInformation("Top up from a Caspian card: {Amount} AZN, source card {SourceCardId}, target card {TargetCardId}, user {UserId}", amount, source.Id, card.Id, userId);

                if (source.UserId != userId)
                {
                    await _notifications.NotifyAsync(source.UserId, NotificationCategory.Card, "Money left your card",
                        $"{amount.ToString("N2", CultureInfo.InvariantCulture)} AZN was taken from your card •••• {source.CardNumber[^4..]} to top up another card.", $"/App/Card/{source.Id}");
                }

                return ServiceResult<CardDto>.Ok(ToDto(card, HolderName(user), card.CardDesign.Image));
            }

            if (await _directory.FindExternalCardAsync(digits) is null)
            {
                return ServiceResult<CardDto>.Fail("We couldn't find a card with that number.");
            }

            _logger.LogInformation("Top up from another bank card: {Amount} AZN, target card {TargetCardId}, user {UserId}", amount, card.Id, userId);
            var description = $"Top up from card •••• {digits[^4..]}";
            if (!await _cards.TopUpAsync(card, amount, description))
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

        // ---- Kartı OTP kodu ilə bloklamaq / blokdan çıxarmaq ----
        // Hər iki əməliyyat iki addımdır: 1) kod hesabın emailinə göndərilir, 2) istifadəçi kodu yazır

        public async Task<ServiceResult<CardCodeSentDto>> SendBlockCodeAsync(string userId, int cardId)
        {
            var user = await _accounts.GetByIdAsync(userId);
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (user is null || card is null)
            {
                return ServiceResult<CardCodeSentDto>.NotFound();
            }

            // Cashback kartı yalnız cashback yığır, ona görə bloklanmır (blok olsa cashback düşə bilməzdi)
            if (card.Tier == CardTier.Cashback)
            {
                return ServiceResult<CardCodeSentDto>.Fail("The Cashback card can't be blocked.");
            }

            if (card.IsBlocked)
            {
                return ServiceResult<CardCodeSentDto>.Fail("This card is already blocked.");
            }

            return await SendCardCodeAsync(user, OtpPurposes.CardBlock(card.Id),
                "Your Caspian Bank card block code",
                $"Use this code to block your card •••• {card.CardNumber[^4..]}:");
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

            if (card.Tier == CardTier.Cashback)
            {
                return ServiceResult<CardDto>.Fail("The Cashback card can't be blocked.");
            }

            if (card.IsBlocked)
            {
                return ServiceResult<CardDto>.Fail("This card is already blocked.");
            }

            // Kod bu kart üçün göndərilməlidir (OtpPurposes.CardBlock(cardId)); 5 yanlış cəhddən sonra kod ləğv olunur
            var confirmed = await _otp.ConfirmCodeAsync(user.Email!, OtpPurposes.CardBlock(card.Id), model.Code);
            if (!confirmed.IsSuccess)
            {
                return ServiceResult<CardDto>.Fail(confirmed.Errors);
            }

            card.IsBlocked = true;
            await _cards.UpdateAsync(card);

            await _notifications.NotifyAsync(userId, NotificationCategory.Card, "Card blocked",
                $"Your card •••• {card.CardNumber[^4..]} was blocked. You can unblock it with a code sent to your email.", $"/App/Card/{card.Id}");

            return ServiceResult<CardDto>.Ok(ToDto(card, HolderName(user), card.CardDesign.Image));
        }

        public async Task<ServiceResult<CardCodeSentDto>> SendUnblockCodeAsync(string userId, int cardId)
        {
            var user = await _accounts.GetByIdAsync(userId);
            var card = await _cards.GetByIdForUserAsync(cardId, userId);
            if (user is null || card is null)
            {
                return ServiceResult<CardCodeSentDto>.NotFound();
            }

            if (!card.IsBlocked)
            {
                return ServiceResult<CardCodeSentDto>.Fail("This card is not blocked.");
            }

            if (user.IsRestricted)
            {
                return ServiceResult<CardCodeSentDto>.Fail("This account is restricted. Contact the bank.");
            }

            return await SendCardCodeAsync(user, OtpPurposes.CardUnblock(card.Id),
                "Your Caspian Bank card unblock code",
                $"Use this code to unblock your card •••• {card.CardNumber[^4..]}:");
        }

        public async Task<ServiceResult<CardDto>> UnblockAsync(string userId, int cardId, UnblockCardDto model)
        {
            var validation = await _unblockValidator.ValidateAsync(model);
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

            if (!card.IsBlocked)
            {
                return ServiceResult<CardDto>.Fail("This card is not blocked.");
            }

            if (user.IsRestricted)
            {
                return ServiceResult<CardDto>.Fail("This account is restricted. Contact the bank.");
            }

            var confirmed = await _otp.ConfirmCodeAsync(user.Email!, OtpPurposes.CardUnblock(card.Id), model.Code);
            if (!confirmed.IsSuccess)
            {
                return ServiceResult<CardDto>.Fail(confirmed.Errors);
            }

            card.IsBlocked = false;
            await _cards.UpdateAsync(card);

            await _notifications.NotifyAsync(userId, NotificationCategory.Card, "Card unblocked",
                $"Your card •••• {card.CardNumber[^4..]} is active again.", $"/App/Card/{card.Id}");

            return ServiceResult<CardDto>.Ok(ToDto(card, HolderName(user), card.CardDesign.Image));
        }

        // Kodu hesabın emailinə göndərir; gözləmə müddəti bitməyibsə qalan saniyə mesajda yazılır
        private async Task<ServiceResult<CardCodeSentDto>> SendCardCodeAsync(AppUser user, string purpose, string subject, string intro)
        {
            var email = user.Email ?? string.Empty;
            var sent = await _otp.SendCodeAsync(email, purpose, subject, intro);
            if (!sent.IsSuccess)
            {
                return ServiceResult<CardCodeSentDto>.Fail(sent.Errors);
            }

            return ServiceResult<CardCodeSentDto>.Ok(new CardCodeSentDto
            {
                MaskedEmail = MaskEmail(email),
                DevCode = sent.DevCode
            });
        }

        // "nurlan@mail.com" → "n****@mail.com"
        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            return parts.Length == 2 && parts[0].Length > 0 ? parts[0][..1] + "****@" + parts[1] : email;
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
