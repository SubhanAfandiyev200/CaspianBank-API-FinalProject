using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Domain.Transfers;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Helpers.Validators.Cards;
using Service.Services.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services
{
    public class RecurringTransferService : IRecurringTransferService
    {
        private const int MaxPerUser = 10;
        private const decimal MaxAmount = 100000m;

        private readonly IRecurringTransferRepository _recurring;
        private readonly ITransferService _transfers;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly ILogger<RecurringTransferService> _logger;

        public RecurringTransferService(IRecurringTransferRepository recurring,
                                        ITransferService transfers,
                                        IAccountRepository accounts,
                                        INotificationService notifications,
                                        ILogger<RecurringTransferService> logger)
        {
            _recurring = recurring;
            _transfers = transfers;
            _accounts = accounts;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<IEnumerable<RecurringTransferDto>> GetAsync(string userId)
        {
            var items = await _recurring.GetByUserAsync(userId);
            var today = Today();
            return items.Select(i => ToDto(i, today)).ToList();
        }

        public async Task<ServiceResult<RecurringTransferDto>> CreateAsync(string userId, CreateRecurringTransferDto model)
        {
            if (model.FromCardId <= 0)
            {
                return ServiceResult<RecurringTransferDto>.Fail("Choose the card to send from.");
            }
            if (model.Amount <= 0m || model.Amount > MaxAmount || decimal.Round(model.Amount, 2) != model.Amount)
            {
                return ServiceResult<RecurringTransferDto>.Fail("Enter an amount between 0.01 and 100,000 AZN with at most two decimal places.");
            }
            if (model.DayOfMonth < RecurringTransferRules.MinDay || model.DayOfMonth > RecurringTransferRules.MaxDay)
            {
                return ServiceResult<RecurringTransferDto>.Fail("Choose a day between 1 and 28.");
            }
            if (model.ToCardId.HasValue == !string.IsNullOrWhiteSpace(model.ToCardNumber))
            {
                return ServiceResult<RecurringTransferDto>.Fail("Choose one of your cards or enter the recipient's card number.");
            }
            var note = model.Note?.Trim();
            if (note is not null && note.Length > 140)
            {
                return ServiceResult<RecurringTransferDto>.Fail("The note can be at most 140 characters.");
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<RecurringTransferDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<RecurringTransferDto>.Fail("Your account is restricted. Please contact support.");
            }
            if (await _recurring.CountByUserAsync(userId) >= MaxPerUser)
            {
                return ServiceResult<RecurringTransferDto>.Fail("You can have at most " + MaxPerUser + " monthly transfers. Delete one to add another.");
            }

            var number = string.IsNullOrWhiteSpace(model.ToCardNumber) ? null : TopUpDtoValidator.Digits(model.ToCardNumber);

            // Alıcını və kartı adi köçürmənin qaydaları ilə yoxlayırıq (1 AZN ilə: balans və məbləğ hədləri indi maraqlı deyil, hər ay yoxlanacaq)
            var check = await _transfers.PreviewAsync(userId, new TransferDto
            {
                FromCardId = model.FromCardId,
                ToCardId = model.ToCardId,
                ToCardNumber = number,
                Amount = 1m
            });
            if (!check.IsSuccess)
            {
                return ServiceResult<RecurringTransferDto>.Fail(check.Errors);
            }

            var today = Today();
            var item = new RecurringTransfer
            {
                UserId = userId,
                SourceCardId = model.FromCardId,
                ToCardId = model.ToCardId,
                ToCardNumber = number,
                ToLabel = check.Data!.ToHolder is null ? check.Data.ToLabel : check.Data.ToHolder + " · " + check.Data.ToLabel,
                Amount = model.Amount,
                Note = string.IsNullOrWhiteSpace(note) ? null : note,
                DayOfMonth = model.DayOfMonth,
                IsActive = true,
                LastPeriod = RecurringTransferRules.InitialLastPeriod(today, model.DayOfMonth)
            };
            await _recurring.AddAsync(item);

            var created = await _recurring.GetForUserAsync(item.Id, userId);
            return ServiceResult<RecurringTransferDto>.Ok(ToDto(created!, today));
        }

        public async Task<ServiceResult<RecurringTransferDto>> SetActiveAsync(string userId, int id, bool active)
        {
            var item = await _recurring.GetForUserAsync(id, userId);
            if (item is null)
            {
                return ServiceResult<RecurringTransferDto>.NotFound();
            }

            var today = Today();
            item.IsActive = active;
            if (active)
            {
                // Davam etdirəndə dayandırılan aylara görə dərhal köçürmə olmasın: növbəti gələn seçilmiş gündən
                item.LastPeriod = RecurringTransferRules.InitialLastPeriod(today, item.DayOfMonth);
                item.LastError = null;
                item.LastAttemptDay = null;
            }
            await _recurring.SaveAsync(item);
            return ServiceResult<RecurringTransferDto>.Ok(ToDto(item, today));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(string userId, int id)
        {
            var item = await _recurring.GetForUserAsync(id, userId);
            if (item is null)
            {
                return ServiceResult<bool>.NotFound();
            }
            await _recurring.DeleteAsync(item);
            return ServiceResult<bool>.Ok(true);
        }

        // ---------- Arxa plan işi ----------

        public async Task<int> RunDueAsync()
        {
            var today = Today();
            var period = RecurringTransferRules.PeriodOf(today);
            var ids = await _recurring.GetDueIdsAsync(period);

            var sent = 0;
            foreach (var id in ids)
            {
                try
                {
                    if (await RunOneAsync(id, today, period))
                    {
                        sent++;
                    }
                }
                catch (Exception ex)
                {
                    // Bir köçürmənin xətası qalanlara mane olmasın, növbəti saatda təkrar yoxlanacaq
                    _logger.LogError(ex, "Aylıq köçürmə {Id} işlənmədi.", id);
                }
            }
            return sent;
        }

        private async Task<bool> RunOneAsync(int id, DateTime today, string period)
        {
            var item = await _recurring.GetForRunAsync(id);
            if (item is null || !item.IsActive || !RecurringTransferRules.IsDue(today, item.DayOfMonth, item.LastPeriod))
            {
                return false;
            }

            // Uğursuz olubsa gündə bir dəfə cəhd edilir (müştəri kartı doldurub ertəsi gün köçürmənin keçməsini gözləyir)
            var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(item.LastError) && item.LastAttemptDay == todayText)
            {
                return false;
            }

            // Eyni köçürmə eyni ay üçün həmişə eyni RequestId alır: təkrar işləsə də pul bir dəfə köçür
            var result = await _transfers.TransferAsync(item.UserId, new TransferDto
            {
                FromCardId = item.SourceCardId,
                ToCardId = item.ToCardId,
                ToCardNumber = item.ToCardNumber,
                Amount = item.Amount,
                Note = item.Note,
                RequestId = RequestIdFor(item.Id, period).ToString()
            });

            // Köçürmə kontekstin izləməsini təmizləyir: dəyişikliyin yazılması üçün sətir təzədən oxunur
            var amountText = item.Amount.ToString("N2", CultureInfo.InvariantCulture) + " AZN";
            item = await _recurring.GetForRunAsync(id);
            if (item is null)
            {
                return result.IsSuccess;
            }

            if (result.IsSuccess)
            {
                item.LastPeriod = period;
                item.LastError = null;
                item.LastAttemptDay = todayText;
                await _recurring.SaveAsync(item);
                await _notifications.NotifyAsync(item.UserId, NotificationCategory.Transfer, "Monthly transfer sent",
                    amountText + " was sent to " + item.ToLabel + ".", "/App/Recurring", "rectr-ok:" + item.Id + ":" + period);
                return true;
            }

            item.LastError = string.Join(" ", result.Errors);
            item.LastAttemptDay = todayText;
            await _recurring.SaveAsync(item);
            await _notifications.NotifyAsync(item.UserId, NotificationCategory.Transfer, "Monthly transfer failed",
                "We couldn't send " + amountText + " to " + item.ToLabel + ": " + item.LastError + " We will try again tomorrow.",
                "/App/Recurring", "rectr-fail:" + item.Id + ":" + todayText);
            return false;
        }

        // ---------- köməkçilər ----------

        private static DateTime Today()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow);
        }

        private static Guid RequestIdFor(int id, string period)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes("recurring-transfer-" + id + "-" + period));
            return new Guid(hash);
        }

        private static RecurringTransferDto ToDto(RecurringTransfer item, DateTime today)
        {
            var card = item.SourceCard;
            return new RecurringTransferDto
            {
                Id = item.Id,
                FromLabel = card.Tier + " •••• " + card.CardNumber.Substring(card.CardNumber.Length - 4),
                ToLabel = item.ToLabel,
                Amount = item.Amount,
                DayOfMonth = item.DayOfMonth,
                Note = item.Note,
                IsActive = item.IsActive,
                NextRunDate = RecurringTransferRules.NextRun(today, item.DayOfMonth, item.LastPeriod),
                LastError = item.LastError
            };
        }
    }
}
