using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services
{
    public class RecurringPaymentService : IRecurringPaymentService
    {
        private const int MaxPerUser = 10;

        private readonly IRecurringPaymentRepository _recurring;
        private readonly ICardRepository _cards;
        private readonly IAccountRepository _accounts;
        private readonly IBillService _bills;
        private readonly ILoanService _loans;
        private readonly INotificationService _notifications;
        private readonly IValidator<CreateRecurringPaymentDto> _createValidator;
        private readonly ILogger<RecurringPaymentService> _logger;

        public RecurringPaymentService(IRecurringPaymentRepository recurring,
                                       ICardRepository cards,
                                       IAccountRepository accounts,
                                       IBillService bills,
                                       ILoanService loans,
                                       INotificationService notifications,
                                       IValidator<CreateRecurringPaymentDto> createValidator,
                                       ILogger<RecurringPaymentService> logger)
        {
            _recurring = recurring;
            _cards = cards;
            _accounts = accounts;
            _bills = bills;
            _loans = loans;
            _notifications = notifications;
            _createValidator = createValidator;
            _logger = logger;
        }

        private static DateTime Today()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow);
        }

        // ---------- Siyahı ----------

        public async Task<IEnumerable<RecurringPaymentDto>> GetAllAsync(string userId)
        {
            var today = Today();
            var period = BillingEngine.PeriodOf(today);
            var items = await _recurring.GetByUserAsync(userId);
            return items.Select(r => RecurringMapper.ToDto(r, today, period)).ToList();
        }

        // ---------- Yaratmaq ----------

        public async Task<ServiceResult<RecurringPaymentDto>> CreateAsync(string userId, CreateRecurringPaymentDto model)
        {
            var validation = await _createValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<RecurringPaymentDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<RecurringPaymentDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<RecurringPaymentDto>.Fail("Your account is restricted. Please contact support.");
            }

            var card = await _cards.GetByIdForUserAsync(model.CardId, userId);
            if (card is null || card.IsBlocked)
            {
                return ServiceResult<RecurringPaymentDto>.Fail("Choose one of your own active cards.");
            }

            int? providerId;
            string identifier;
            decimal amount;

            if (!model.ProviderId.HasValue || model.ProviderId.Value <= 0)
            {
                // Kredit: kod açıq krediti göstərməlidir. Məbləğ yoxdur, hər ay həmin ayın aylıq ödənişi ödənilir
                var code = model.Identifier.Trim().ToUpperInvariant();
                var loan = await _loans.LookupAsync(code);
                if (loan.IsNotFound)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail("We couldn't find an active loan with this code.");
                }
                if (!loan.IsSuccess)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail(loan.Errors);
                }

                providerId = null;
                identifier = loan.Data!.ContractNumber;
                amount = 0m;
            }
            else
            {
                // Nömrə və ya abunəçi kodu adi axtarışla yoxlanır (səhv operator, olmayan nömrə və s. eyni mesajlarla)
                var lookup = await _bills.LookupAsync(new LookupBillDto
                {
                    ProviderId = model.ProviderId.Value,
                    Identifier = model.Identifier
                });
                if (lookup.IsNotFound)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail("This provider isn't available right now.");
                }
                if (!lookup.IsSuccess)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail(lookup.Errors);
                }

                var found = lookup.Data!;
                amount = decimal.Round(model.Amount, 2);
                if (amount < found.MinPayment || amount > found.MaxPayment)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail(
                        "The amount must be between " + found.MinPayment.ToString("N2", CultureInfo.InvariantCulture)
                        + " and " + found.MaxPayment.ToString("N2", CultureInfo.InvariantCulture) + " AZN.");
                }

                providerId = found.ProviderId;
                identifier = found.Category == nameof(BillCategory.Mobile)
                    ? BillingEngine.NormalizePhone(found.Identifier)!
                    : found.Identifier;
            }

            var period = BillingEngine.PeriodOf(Today());
            var existing = await _recurring.FindAsync(userId, providerId, identifier);
            if (existing is null)
            {
                if (await _recurring.CountByUserAsync(userId) >= MaxPerUser)
                {
                    return ServiceResult<RecurringPaymentDto>.Fail("You can have up to " + MaxPerUser + " monthly payments. Remove one to add another.");
                }

                existing = new RecurringPayment
                {
                    UserId = userId,
                    ProviderId = providerId,
                    Identifier = identifier
                };
                ApplyCreateFields(existing, amount, card.Id, period);
                await _recurring.AddAsync(existing);
            }
            else
            {
                // Eyni ödəniş yenidən seçilib: məbləğ və kart yenilənir, dayandırılıbsa yenidən başlayır
                ApplyCreateFields(existing, amount, card.Id, period);
                await _recurring.SaveAsync(existing);
            }

            var saved = await _recurring.GetOwnedAsync(existing.Id, userId);
            var today = Today();
            return ServiceResult<RecurringPaymentDto>.Ok(RecurringMapper.ToDto(saved!, today, period));
        }

        // İlk ödəniş elə indi edilir, ona görə bu ay üçün işlənmiş sayılır: növbəti ödəniş gələn ayın 1-ində olur
        private static void ApplyCreateFields(RecurringPayment item, decimal amount, int cardId, string period)
        {
            item.Amount = amount;
            item.CardId = cardId;
            item.IsActive = true;
            item.LastPeriod = period;
            item.LastAttemptDay = null;
            item.LastError = null;
        }

        // ---------- Dayandırmaq, yenidən başlatmaq, silmək ----------

        public async Task<ServiceResult<RecurringPaymentDto>> SetActiveAsync(string userId, int id, bool active)
        {
            var item = await _recurring.GetOwnedAsync(id, userId);
            if (item is null)
            {
                return ServiceResult<RecurringPaymentDto>.NotFound();
            }

            item.IsActive = active;
            if (active)
            {
                // Yenidən başladılanda köhnə xəta silinir. Bu ay artıq işlənməyibsə gündəlik iş növbəti dəfə onu edəcək
                item.LastError = null;
                item.LastAttemptDay = null;
            }
            await _recurring.SaveAsync(item);

            var today = Today();
            return ServiceResult<RecurringPaymentDto>.Ok(RecurringMapper.ToDto(item, today, BillingEngine.PeriodOf(today)));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(string userId, int id)
        {
            var item = await _recurring.GetOwnedAsync(id, userId);
            if (item is null)
            {
                return ServiceResult<bool>.NotFound();
            }

            await _recurring.DeleteAsync(item);
            return ServiceResult<bool>.Ok(true);
        }

        // ---------- Gündəlik iş ----------

        public async Task<int> RunDueAsync()
        {
            var today = Today();
            var period = BillingEngine.PeriodOf(today);
            var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var ids = await _recurring.GetDueIdsAsync(period);
            var paid = 0;
            foreach (var id in ids)
            {
                try
                {
                    if (await RunOneAsync(id, period, todayText))
                    {
                        paid++;
                    }
                }
                catch (Exception ex)
                {
                    // Bir ödənişin xətası qalanlara mane olmasın, növbəti saatda təkrar yoxlanacaq
                    _logger.LogError(ex, "Aylıq ödəniş {Id} işlənmədi.", id);
                }
            }
            return paid;
        }

        private async Task<bool> RunOneAsync(int id, string period, string todayText)
        {
            var item = await _recurring.GetForRunAsync(id);
            if (item is null || !item.IsActive || item.LastPeriod == period)
            {
                return false;
            }

            // Uğursuz olubsa gündə bir dəfə cəhd edilir (müştəri kartı doldurub ertəsi gün ödənişin keçməsini gözləyir)
            if (!string.IsNullOrEmpty(item.LastError) && item.LastAttemptDay == todayText)
            {
                return false;
            }

            var requestId = RequestIdFor(item.Id, period);
            var isLoan = item.ProviderId is null;
            var name = isLoan ? RecurringMapper.LoanName : item.Provider!.Name;
            var shown = isLoan ? item.Identifier : BillMapper.DisplayIdentifier(item.Provider!.Category, item.Identifier);
            var cardLabel = LoanMapper.CardLabel(item.Card.CardNumber);

            decimal paidAmount = 0m;
            string[] errors = Array.Empty<string>();
            var succeeded = false;

            if (isLoan)
            {
                var loan = await _loans.LookupAsync(item.Identifier);
                if (loan.IsNotFound)
                {
                    // Kredit bağlanıb (tam ödənilib): aylıq ödəniş artıq lazım deyil, dayandırılır və müştəriyə deyilir
                    item.IsActive = false;
                    item.LastError = "The loan is closed.";
                    item.LastAttemptDay = todayText;
                    await _recurring.SaveAsync(item);
                    await _notifications.NotifyAsync(item.UserId, NotificationCategory.Bill,
                        "Monthly loan payment stopped",
                        "Loan " + item.Identifier + " is closed, so its monthly payment was stopped.",
                        "/App/Bills", "rec-closed:" + item.Id);
                    return false;
                }
                if (loan.IsSuccess && !loan.Data!.CanPay)
                {
                    // Bu ay ödəniləsi bir şey yoxdur (müştəri özü ödəyib): ay işlənmiş sayılır, səssiz
                    item.LastPeriod = period;
                    item.LastError = null;
                    item.LastAttemptDay = todayText;
                    await _recurring.SaveAsync(item);
                    return false;
                }

                var result = await _loans.PayByCodeAsync(item.UserId, new PayByCodeDto
                {
                    ContractNumber = item.Identifier,
                    CardId = item.CardId,
                    RequestId = requestId
                });
                succeeded = result.IsSuccess;
                errors = result.Errors;
                paidAmount = result.IsSuccess ? result.Data!.Amount : 0m;
            }
            else
            {
                var result = await _bills.PayAsync(item.UserId, new PayBillDto
                {
                    ProviderId = item.ProviderId!.Value,
                    Identifier = item.Identifier,
                    Amount = item.Amount,
                    CardId = item.CardId,
                    RequestId = requestId
                });
                succeeded = result.IsSuccess;
                errors = result.Errors;
                paidAmount = result.IsSuccess ? result.Data!.Amount : 0m;
            }

            item.LastAttemptDay = todayText;
            if (succeeded)
            {
                item.LastPeriod = period;
                item.LastError = null;
                await _recurring.SaveAsync(item);

                await _notifications.NotifyAsync(item.UserId, NotificationCategory.Bill,
                    "Monthly payment made",
                    paidAmount.ToString("N2", CultureInfo.InvariantCulture) + " AZN was paid to " + name + " (" + shown + ") from card " + cardLabel + ".",
                    "/App/Bills", "rec-ok:" + item.Id + ":" + period);
                return true;
            }

            var reason = errors.Length > 0 ? errors[0] : "The payment could not be made.";
            item.LastError = reason.Length > 300 ? reason.Substring(0, 300) : reason;
            await _recurring.SaveAsync(item);

            // Ay boyu hər gün cəhd olunur, amma müştəriyə bir dəfə deyilir
            await _notifications.NotifyAsync(item.UserId, NotificationCategory.Bill,
                "Monthly payment failed",
                name + " (" + shown + "): " + reason + " We will try again tomorrow.",
                "/App/Bills", "rec-fail:" + item.Id + ":" + period);
            return false;
        }

        // Eyni ödəniş və ay üçün həmişə eyni RequestId: iş yarımçıq qalıb təkrar işləsə pul ikinci dəfə çıxmır
        private static string RequestIdFor(int id, string period)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes("recurring:" + id + ":" + period));
            return new Guid(hash).ToString();
        }
    }
}
