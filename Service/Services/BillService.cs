using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BillService : IBillService
    {
        private readonly IBillRepository _bills;
        private readonly IAccountRepository _accounts;
        private readonly IValidator<LookupBillDto> _lookupValidator;
        private readonly IValidator<PayBillDto> _payValidator;

        public BillService(IBillRepository bills,
                           IAccountRepository accounts,
                           IValidator<LookupBillDto> lookupValidator,
                           IValidator<PayBillDto> payValidator)
        {
            _bills = bills;
            _accounts = accounts;
            _lookupValidator = lookupValidator;
            _payValidator = payValidator;
        }

        // ---------- Təchizatçılar ----------

        public async Task<IEnumerable<BillProviderDto>> GetProvidersAsync()
        {
            var providers = await _bills.GetProvidersAsync(true);
            return providers.Select(BillMapper.ToProviderDto).ToList();
        }

        // ---------- Axtarış ----------

        public async Task<ServiceResult<BillLookupDto>> LookupAsync(LookupBillDto model)
        {
            var validation = await _lookupValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<BillLookupDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var provider = await _bills.GetProviderAsync(model.ProviderId);
            if (provider is null || !provider.IsActive)
            {
                return ServiceResult<BillLookupDto>.NotFound();
            }

            var (identifier, error) = await NormalizeAsync(provider, model.Identifier);
            if (identifier is null)
            {
                return ServiceResult<BillLookupDto>.Fail(error!);
            }

            var subscriber = await _bills.FindSubscriberAsync(provider.Id, identifier);
            if (subscriber is null)
            {
                return ServiceResult<BillLookupDto>.Fail(BillMessages.SubscriberNotFound(provider));
            }

            var lastCharge = await _bills.GetLastChargeAsync(subscriber.Id);
            return ServiceResult<BillLookupDto>.Ok(BillMapper.ToLookup(provider, subscriber, lastCharge));
        }

        // ---------- Ödəniş ----------

        public async Task<ServiceResult<BillReceiptDto>> PayAsync(string userId, PayBillDto model)
        {
            var validation = await _payValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<BillReceiptDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<BillReceiptDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<BillReceiptDto>.Fail("Your account is restricted. Please contact support.");
            }

            var provider = await _bills.GetProviderAsync(model.ProviderId);
            if (provider is null || !provider.IsActive)
            {
                return ServiceResult<BillReceiptDto>.Fail("This provider isn't available right now.");
            }

            var (identifier, error) = await NormalizeAsync(provider, model.Identifier);
            if (identifier is null)
            {
                return ServiceResult<BillReceiptDto>.Fail(error!);
            }

            var plan = new BillPaymentPlan(
                UserId: userId,
                ProviderId: provider.Id,
                Identifier: identifier,
                Amount: decimal.Round(model.Amount, 2),
                CardId: model.CardId,
                Reference: ReferenceFor(model.RequestId!),
                UtcNow: DateTime.UtcNow);

            var result = await _bills.PayAsync(plan);

            if (result.Outcome == BillOutcome.Duplicate)
            {
                // Forma iki dəfə göndərilib: əvvəlki ödənişin qəbzi qaytarılır, pul ikinci dəfə çıxmır
                var existing = await _bills.FindPaymentAsync(plan.Reference, userId);
                if (existing is null)
                {
                    return ServiceResult<BillReceiptDto>.Fail("This payment was already processed.");
                }
                return ServiceResult<BillReceiptDto>.Ok(BillMapper.ToReceipt(existing));
            }

            if (result.Outcome != BillOutcome.Success)
            {
                return BillMessages.ToFailure<BillReceiptDto>(result);
            }

            return ServiceResult<BillReceiptDto>.Ok(BillMapper.ToReceipt(result));
        }

        // ---------- Xülasə ----------

        public async Task<BillsOverviewDto> GetOverviewAsync(string userId)
        {
            // Ay Bakı vaxtı ilə başlayır: onu UTC-yə çevirmək üçün 4 saat çıxırıq
            var bakuToday = LoanEngine.BakuToday(DateTime.UtcNow);
            var monthStartUtc = new DateTime(bakuToday.Year, bakuToday.Month, 1).AddHours(-4);

            var summary = await _bills.GetSummaryAsync(userId, monthStartUtc);
            var recent = await _bills.GetRecentPaymentsAsync(userId, 10);

            return new BillsOverviewDto
            {
                PaidThisMonth = summary.Paid,
                PaymentsThisMonth = summary.Count,
                CashbackThisMonth = summary.Cashback,
                RecentPayments = recent.Select(BillMapper.ToPaymentDto).ToList()
            };
        }

        // ---------- Gündəlik iş ----------

        public async Task<int> RunMonthlyBillingAsync()
        {
            return await _bills.RunMonthlyBillingAsync(DateTime.UtcNow);
        }

        // ---------- Köməkçilər ----------

        // RequestId-dən kart jurnalında görünən qısa kod (BP-3F9A...). Eyni sorğu həmişə eyni kodu verir
        private static string ReferenceFor(string requestId)
        {
            return "BP-" + Guid.Parse(requestId).ToString("N").Substring(0, 16).ToUpperInvariant();
        }

        // Yazılan nömrəni/kodu yoxlayıb standart formaya salır. Səhv olsa istifadəçiyə aydın mesaj qayıdır
        private async Task<(string? Identifier, string? Error)> NormalizeAsync(BillProvider provider, string raw)
        {
            if (provider.Category == BillCategory.Mobile)
            {
                var phone = BillingEngine.NormalizePhone(raw);
                if (phone is null)
                {
                    return (null, "Enter the phone number like 055 123 45 67.");
                }

                var prefix = BillingEngine.PhonePrefix(phone);
                if (!BillingEngine.PrefixBelongs(provider.Prefixes, prefix))
                {
                    // Səhv operator seçilib: nömrənin əsl operatoru varsa deyirik
                    var providers = await _bills.GetProvidersAsync(true);
                    var other = providers.FirstOrDefault(p => p.Category == BillCategory.Mobile && p.Id != provider.Id && BillingEngine.PrefixBelongs(p.Prefixes, prefix));
                    return (null, BillMessages.WrongOperator(provider, other));
                }
                return (phone, null);
            }

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length != provider.IdentifierLength || digits.Length != raw.Count(c => !char.IsWhiteSpace(c)))
            {
                return (null, "Enter the " + provider.IdentifierLength + "-digit " + provider.IdentifierLabel.ToLowerInvariant() + ".");
            }
            return (digits, null);
        }
    }
}
