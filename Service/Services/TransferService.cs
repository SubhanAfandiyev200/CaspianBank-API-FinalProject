using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Helpers.Validators.Cards;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class TransferService : ITransferService
    {
        private readonly ICardRepository _cards;
        private readonly ICardTierConfigRepository _tiers;
        private readonly ITransferRepository _transfers;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<TransferDto> _validator;

        public TransferService(ICardRepository cards,
                               ICardTierConfigRepository tiers,
                               ITransferRepository transfers,
                               IAccountRepository accounts,
                               IValidator<TransferDto> validator)
        {
            _cards = cards;
            _tiers = tiers;
            _transfers = transfers;
            _accounts = accounts;
            _validator = validator;
        }

        // Yoxlamalardan keçmiş köçürmənin bütün hissələri
        private sealed record Plan(Card Source, Card Destination, AppUser Sender, AppUser Recipient, bool IsOwn, decimal Amount, decimal Commission, string? Note);

        public async Task<ServiceResult<TransferPreviewDto>> PreviewAsync(string userId, TransferDto model)
        {
            var (plan, errors) = await ResolveAsync(userId, model);
            if (plan is null)
            {
                return ServiceResult<TransferPreviewDto>.Fail(errors);
            }

            return ServiceResult<TransferPreviewDto>.Ok(new TransferPreviewDto
            {
                FromLabel = Label(plan.Source),
                FromBalance = plan.Source.Balance,
                ToLabel = plan.IsOwn ? Label(plan.Destination) : $"•••• {plan.Destination.CardNumber[^4..]}",
                ToHolder = plan.IsOwn ? null : MaskedName(plan.Recipient),
                IsOwn = plan.IsOwn,
                Amount = plan.Amount,
                Commission = plan.Commission,
                Total = plan.Amount + plan.Commission,
                Note = plan.Note
            });
        }

        public async Task<ServiceResult<TransferReceiptDto>> TransferAsync(string userId, TransferDto model)
        {
            // Eyni RequestId ilə təkrar sorğu (ikiqat klik, səhifəni yeniləmə): pul yenidən köçürülmür, əvvəlki qəbz qaytarılır
            var requestId = Guid.TryParse(model.RequestId, out var parsed) ? parsed : Guid.NewGuid();
            var reference = "TR-" + requestId.ToString("N")[..12].ToUpperInvariant();

            if (!string.IsNullOrEmpty(model.RequestId))
            {
                var existing = await ExistingReceiptAsync(reference, userId);
                if (existing is not null)
                {
                    return ServiceResult<TransferReceiptDto>.Ok(existing);
                }
            }

            var (plan, errors) = await ResolveAsync(userId, model);
            if (plan is null)
            {
                return ServiceResult<TransferReceiptDto>.Fail(errors);
            }


            var last4 = plan.Destination.CardNumber[^4..];
            var descriptionOut = plan.IsOwn ? $"Transfer to your {plan.Destination.Tier} card •••• {last4}" : $"Transfer to •••• {last4}";
            var descriptionIn = plan.IsOwn ? $"Transfer from your {plan.Source.Tier} card •••• {plan.Source.CardNumber[^4..]}" : $"Transfer from {MaskedName(plan.Sender)}";

            var result = await _transfers.ExecuteAsync(new TransferPlan(
                userId, plan.Source.Id, plan.Destination.Id, plan.Amount, plan.Commission,
                reference, plan.Note, descriptionOut, descriptionIn));

            switch (result.Outcome)
            {
                case TransferOutcome.Success:
                    return ServiceResult<TransferReceiptDto>.Ok(new TransferReceiptDto
                    {
                        Reference = reference,
                        FromLabel = Label(plan.Source),
                        ToLabel = plan.IsOwn ? Label(plan.Destination) : $"•••• {last4}",
                        ToHolder = plan.IsOwn ? null : MaskedName(plan.Recipient),
                        Amount = plan.Amount,
                        Commission = plan.Commission,
                        Total = plan.Amount + plan.Commission,
                        Note = plan.Note,
                        FromBalanceAfter = result.SourceBalanceAfter,
                        CreatedAt = DateTime.UtcNow
                    });
                case TransferOutcome.InsufficientFunds:
                    return ServiceResult<TransferReceiptDto>.Fail("The source card doesn't cover the amount and commission.");
                case TransferOutcome.SourceBlocked:
                    return ServiceResult<TransferReceiptDto>.Fail("The source card is blocked.");
                case TransferOutcome.DestinationBlocked:
                    return ServiceResult<TransferReceiptDto>.Fail("The destination card is blocked.");
                case TransferOutcome.NotAllowed:
                    return ServiceResult<TransferReceiptDto>.Fail("This transfer isn't allowed for the selected cards.");
                case TransferOutcome.Duplicate:
                    // İki sorğu eyni anda gəlib: biri yazılıb, digəri unikal indeksə düşüb: əvvəlki qəbz qaytarılır
                    var original = await ExistingReceiptAsync(reference, userId);
                    if (original is not null)
                    {
                        return ServiceResult<TransferReceiptDto>.Ok(original);
                    }
                    return ServiceResult<TransferReceiptDto>.Fail("This transfer has already been submitted.");
                case TransferOutcome.Conflict:
                    return ServiceResult<TransferReceiptDto>.Fail("The balance changed while the transfer was processed. Please try again.");
                default:
                    return ServiceResult<TransferReceiptDto>.Fail("The cards for this transfer could not be found.");
            }
        }

        // Əvvəl icra olunmuş köçürmənin qəbzi (yoxdursa null)
        private async Task<TransferReceiptDto?> ExistingReceiptAsync(string reference, string userId)
        {
            var record = await _transfers.FindAsync(reference, userId);
            if (record is null)
            {
                return null;
            }

            var isOwn = record.Destination.UserId == userId;
            string? holder = null;
            if (!isOwn)
            {
                var recipient = await _accounts.GetByIdAsync(record.Destination.UserId);
                holder = recipient is null ? null : MaskedName(recipient);
            }

            return new TransferReceiptDto
            {
                Reference = reference,
                FromLabel = Label(record.Source),
                ToLabel = isOwn ? Label(record.Destination) : $"•••• {record.Destination.CardNumber[^4..]}",
                ToHolder = holder,
                Amount = record.Amount,
                Commission = record.Commission,
                Total = record.Amount + record.Commission,
                Note = record.Note,
                FromBalanceAfter = record.SourceBalanceAfter,
                CreatedAt = record.CreatedAt
            };
        }

        // ---- ortaq yoxlama ----

        private async Task<(Plan? Plan, string[] Errors)> ResolveAsync(string userId, TransferDto model)
        {
            var validation = await _validator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return (null, validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var sender = await _accounts.GetByIdAsync(userId);
            if (sender is null)
            {
                return (null, new[] { "Please log in again." });
            }

            if (sender.IsRestricted)
            {
                return (null, new[] { "This account is restricted. Contact the bank." });
            }

            var source = await _cards.GetByIdForUserAsync(model.FromCardId, userId);
            if (source is null)
            {
                return (null, new[] { "Choose one of your own cards to send from." });
            }
            if (source.IsBlocked)
            {
                return (null, new[] { "The source card is blocked." });
            }

            Card? destination;
            if (model.ToCardId.HasValue)
            {
                destination = await _cards.GetByIdForUserAsync(model.ToCardId.Value, userId);
                if (destination is null)
                {
                    return (null, new[] { "Choose another of your own cards, or send to another customer." });
                }
            }
            else
            {
                destination = await _cards.GetByNumberAsync(TopUpDtoValidator.Digits(model.ToCardNumber!));
                if (destination is null)
                {
                    return (null, new[] { "We couldn't find a card with that number." });
                }
            }

            if (destination.Id == source.Id)
            {
                return (null, new[] { "Choose a different card from the one you're sending from." });
            }
            if (destination.IsBlocked)
            {
                return (null, new[] { "The destination card is blocked." });
            }

            var isOwn = destination.UserId == userId;

            // Cashback kartına heç bir köçürmə olmur (o yalnız cashback yığır). Cashback kartından isə yalnız öz kartlarına köçürmək olar
            if (destination.Tier == CardTier.Cashback)
            {
                var message = isOwn ? "The Cashback card only collects cashback, so money can't be transferred to it." : "This card can't receive transfers.";
                return (null, new[] { message });
            }
            if (!isOwn && source.Tier == CardTier.Cashback)
            {
                return (null, new[] { "The Cashback card can only send money to your own cards." });
            }

            var recipient = isOwn ? sender : await _accounts.GetByIdAsync(destination.UserId);
            if (recipient is null)
            {
                return (null, new[] { "We couldn't find a card with that number." });
            }

            var amount = decimal.Round(model.Amount, 2);
            var commission = 0m;

            // Komissiya hər köçürməyə (öz kartlarına da) tətbiq olunur: göndərən kartın limitindən yuxarı olan hissəyə
            var config = await _tiers.GetByTierAsync(source.Tier);
            if (config is null)
            {
                return (null, new[] { "Transfers are not available right now. Please try again later." });
            }

            if (amount > config.TransferLimit)
            {
                commission = decimal.Round((amount - config.TransferLimit) * config.CommissionPercent / 100m, 2, MidpointRounding.AwayFromZero);
            }

            if (source.Balance < amount + commission)
            {
                return (null, new[] { "The source card doesn't cover the amount and commission." });
            }

            var note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
            return (new Plan(source, destination, sender, recipient, isOwn, amount, commission, note), Array.Empty<string>());
        }

        private static string Label(Card card)
        {
            return $"{card.Tier} •••• {card.CardNumber[^4..]}";
        }

        // "Nurlan Efendiyev" → "Nurlan E." (tam adı başqasına göstərmirik)
        private static string MaskedName(AppUser user)
        {
            var surname = user.Surname.Trim();
            return surname.Length == 0 ? user.Name.Trim() : $"{user.Name.Trim()} {surname[0]}.";
        }
    }
}
