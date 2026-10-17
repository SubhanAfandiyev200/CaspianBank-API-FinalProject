using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Domain.Transfers;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Helpers.Validators.Cards;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class TransferService : ITransferService
    {
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);

        private readonly ICardRepository _cards;
        private readonly ICardTierConfigRepository _tiers;
        private readonly ITransferRepository _transfers;
        private readonly IAccountRepository _accounts;
        private readonly IBankDirectoryRepository _directory;
        private readonly IBaseRepository<OwnBank> _ownBanks;
        private readonly IValidator<TransferDto> _validator;
        private readonly INotificationService _notifications;
        private readonly ISecurityMonitor _security;

        public TransferService(ICardRepository cards,
                               ICardTierConfigRepository tiers,
                               ITransferRepository transfers,
                               IAccountRepository accounts,
                               IBankDirectoryRepository directory,
                               IBaseRepository<OwnBank> ownBanks,
                               IValidator<TransferDto> validator,
                               INotificationService notifications,
                               ISecurityMonitor security)
        {
            _cards = cards;
            _tiers = tiers;
            _transfers = transfers;
            _accounts = accounts;
            _directory = directory;
            _ownBanks = ownBanks;
            _validator = validator;
            _notifications = notifications;
            _security = security;
        }

        // Yoxlamalardan keçmiş köçürmənin bütün hissələri. Başqa banka gedəndə Destination və Recipient boşdur, External dolu olur
        private sealed record Plan(Card Source, Card? Destination, ExternalCard? External, AppUser Sender, AppUser? Recipient, bool IsOwn,
                                   decimal Amount, decimal Commission, decimal UsedThisMonth, decimal MonthlyLimit, decimal ChargedAmount, string? Note);

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
                ToLabel = ToLabel(plan),
                ToHolder = ToHolder(plan),
                ToBankName = plan.External is null ? null : BankName(plan.External),
                IsExternal = plan.External is not null,
                IsOwn = plan.IsOwn,
                Amount = plan.Amount,
                Commission = plan.Commission,
                Total = plan.Amount + plan.Commission,
                Note = plan.Note,
                MonthlyLimit = plan.MonthlyLimit,
                UsedThisMonth = plan.UsedThisMonth,
                ChargedAmount = plan.ChargedAmount
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

            TransferResult result;
            string last4;
            if (plan.External is not null)
            {
                last4 = plan.External.CardNumber[^4..];
                var bank = BankName(plan.External);
                result = await _transfers.ExecuteExternalAsync(new ExternalTransferPlan(
                    userId, plan.Source.Id, plan.External.Id, bank, last4, ToHolder(plan)!, plan.Amount, plan.Commission,
                    reference, plan.Note, $"Transfer to {bank} •••• {last4}"));
            }
            else
            {
                last4 = plan.Destination!.CardNumber[^4..];
                var descriptionOut = plan.IsOwn ? $"Transfer to your {plan.Destination.Tier} card •••• {last4}" : $"Transfer to •••• {last4}";
                var descriptionIn = plan.IsOwn ? $"Transfer from your {plan.Source.Tier} card •••• {plan.Source.CardNumber[^4..]}" : $"Transfer from {MaskedName(plan.Sender)}";

                result = await _transfers.ExecuteAsync(new TransferPlan(
                    userId, plan.Source.Id, plan.Destination.Id, plan.Amount, plan.Commission,
                    reference, plan.Note, descriptionOut, descriptionIn));
            }

            switch (result.Outcome)
            {
                case TransferOutcome.Success:
                    // Başqa Caspian müştərisinin kartına gedibsə, alıcı pulun gəldiyini dərhal görür (öz kartları arasında və başqa banka bildiriş yoxdur)
                    if (!plan.IsOwn && plan.Destination is not null && plan.Recipient is not null)
                    {
                        await _notifications.NotifyAsync(
                            plan.Recipient.Id,
                            NotificationCategory.Transfer,
                            "Money received",
                            $"You received {plan.Amount.ToString("N2", CultureInfo.InvariantCulture)} AZN from {MaskedName(plan.Sender)} on your card •••• {last4}.",
                            $"/App/Card/{plan.Destination.Id}");
                    }
                    // Başqasına göndərilən pul şübhəli fəaliyyət qaydalarından keçir (öz kartları arasında köçürmə sayılmır). Xəta köçürməni pozmur
                    if (!plan.IsOwn)
                    {
                        await _security.EvaluateTransferAsync(userId, plan.Amount, reference);
                    }
                    return ServiceResult<TransferReceiptDto>.Ok(new TransferReceiptDto
                    {
                        Reference = reference,
                        FromLabel = Label(plan.Source),
                        ToLabel = ToLabel(plan),
                        ToHolder = ToHolder(plan),
                        ToBankName = plan.External is null ? null : BankName(plan.External),
                        IsExternal = plan.External is not null,
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

        public async Task<IEnumerable<CardLimitDto>> GetLimitsAsync(string userId)
        {
            var (fromUtc, toUtc) = CurrentMonthRange();
            var cards = await _cards.GetByUserAsync(userId);
            var result = new List<CardLimitDto>();
            foreach (var card in cards)
            {
                var config = await _tiers.GetByTierAsync(card.Tier);
                if (config is null)
                {
                    continue;
                }

                result.Add(new CardLimitDto
                {
                    CardId = card.Id,
                    Tier = card.Tier.ToString(),
                    MonthlyLimit = config.TransferLimit,
                    CommissionPercent = config.CommissionPercent,
                    UsedThisMonth = await _transfers.SumSentAsync(card.Id, fromUtc, toUtc)
                });
            }
            return result;
        }

        // Əvvəl icra olunmuş köçürmənin qəbzi (yoxdursa null)
        private async Task<TransferReceiptDto?> ExistingReceiptAsync(string reference, string userId)
        {
            var record = await _transfers.FindAsync(reference, userId);
            if (record is null)
            {
                // Başqa bankın kartına köçürmə
                var external = await _transfers.FindExternalAsync(reference, userId);
                if (external is null)
                {
                    return null;
                }

                return new TransferReceiptDto
                {
                    Reference = reference,
                    FromLabel = Label(external.SourceCard),
                    ToLabel = $"{external.BankName} •••• {external.CardLast4}",
                    ToHolder = external.HolderMasked,
                    ToBankName = external.BankName,
                    IsExternal = true,
                    Amount = external.Amount,
                    Commission = external.Commission,
                    Total = external.Amount + external.Commission,
                    Note = external.Note,
                    FromBalanceAfter = external.SourceBalanceAfter,
                    CreatedAt = external.CreatedAt
                };
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

            Card? destination = null;
            ExternalCard? external = null;
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
                var number = TopUpDtoValidator.Digits(model.ToCardNumber!);
                destination = await _cards.GetByNumberAsync(number);
                if (destination is null)
                {
                    // Caspian kartı deyil: başqa bankın kartıdırmı
                    external = await _directory.FindExternalCardAsync(number);
                    if (external is null)
                    {
                        return (null, new[] { await NotFoundMessageAsync(number) });
                    }
                }
            }

            var isOwn = false;
            AppUser? recipient = null;

            if (destination is not null)
            {
                if (destination.Id == source.Id)
                {
                    return (null, new[] { "Choose a different card from the one you're sending from." });
                }
                if (destination.IsBlocked)
                {
                    return (null, new[] { "The destination card is blocked." });
                }

                isOwn = destination.UserId == userId;

                // Cashback kartına heç bir köçürmə olmur (o yalnız cashback yığır). Cashback kartından isə istənilən karta köçürmək olar
                if (destination.Tier == CardTier.Cashback)
                {
                    var message = isOwn ? "The Cashback card only collects cashback, so money can't be transferred to it." : "This card can't receive transfers.";
                    return (null, new[] { message });
                }

                recipient = isOwn ? sender : await _accounts.GetByIdAsync(destination.UserId);
                if (recipient is null)
                {
                    return (null, new[] { "We couldn't find a card with that number." });
                }
            }

            var amount = decimal.Round(model.Amount, 2);

            // Başqa banka bir dəfəyə göndərilə bilən ən çox məbləğ (bank qaydası)
            if (external is not null)
            {
                var bank = await _ownBanks.GetAsync();
                if (bank is not null && bank.MaxInterbankAmount > 0m && amount > bank.MaxInterbankAmount)
                {
                    return (null, new[] { "You can send at most " + bank.MaxInterbankAmount.ToString("N2", CultureInfo.InvariantCulture) + " AZN to another bank at once." });
                }
            }

            // Komissiya yalnız kartın AYLIQ pulsuz limitini keçən hissəyə tutulur (bütün köçürmə növlərində eyni, ayrıca banklararası komissiya yoxdur)
            var config = await _tiers.GetByTierAsync(source.Tier);
            if (config is null)
            {
                return (null, new[] { "Transfers are not available right now. Please try again later." });
            }

            var (fromUtc, toUtc) = CurrentMonthRange();
            var used = await _transfers.SumSentAsync(source.Id, fromUtc, toUtc);
            var charged = TransferFees.ChargeableAmount(used, amount, config.TransferLimit);
            var commission = TransferFees.Commission(used, amount, config.TransferLimit, config.CommissionPercent);

            if (source.Balance < amount + commission)
            {
                return (null, new[] { "The source card doesn't cover the amount and commission." });
            }

            var note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
            return (new Plan(source, destination, external, sender, recipient, isOwn, amount, commission, used, config.TransferLimit, charged, note), Array.Empty<string>());
        }

        // Kart tapılmayanda: bankı tanınırsa hansı bankda tapılmadığı deyilir
        private async Task<string> NotFoundMessageAsync(string number)
        {
            if (number.Length >= 6)
            {
                var bank = await _directory.FindExternalBankByBinAsync(number.Substring(0, 6));
                if (bank is not null)
                {
                    return $"We couldn't find a card with that number at {BankShort(bank.Name)}.";
                }
            }
            return "We couldn't find a card with that number.";
        }

        // Bu ay Bakı vaxtı ilə: [ayın 1-i 00:00, gələn ayın 1-i 00:00) UTC-də
        private static (DateTime FromUtc, DateTime ToUtc) CurrentMonthRange()
        {
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var start = new DateTime(today.Year, today.Month, 1);
            return (DateTime.SpecifyKind(start - BakuOffset, DateTimeKind.Utc), DateTime.SpecifyKind(start.AddMonths(1) - BakuOffset, DateTimeKind.Utc));
        }

        private static string ToLabel(Plan plan)
        {
            if (plan.External is not null)
            {
                return $"{BankName(plan.External)} •••• {plan.External.CardNumber[^4..]}";
            }
            return plan.IsOwn ? Label(plan.Destination!) : $"•••• {plan.Destination!.CardNumber[^4..]}";
        }

        private static string? ToHolder(Plan plan)
        {
            if (plan.External is not null)
            {
                return MaskedHolder(plan.External.HolderName);
            }
            return plan.IsOwn ? null : MaskedName(plan.Recipient!);
        }

        private static string BankName(ExternalCard card)
        {
            return BankShort(card.ExternalBank.Name);
        }

        // "International Bank of Azerbaijan (ABB)" -> "ABB"
        private static string BankShort(string name)
        {
            var open = name.LastIndexOf('(');
            var close = name.LastIndexOf(')');
            if (open >= 0 && close > open)
            {
                return name.Substring(open + 1, close - open - 1).Trim();
            }
            return name;
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

        // "Elnur Məmmədov" → "Elnur M."
        private static string MaskedHolder(string holder)
        {
            var parts = holder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return holder.Trim();
            }
            return $"{parts[0]} {parts[parts.Length - 1][0]}.";
        }
    }
}
