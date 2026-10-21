using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class LoanService : ILoanService
    {
        // Müddət seçimləri (dizayndakı kimi); bankın min/max müddətinə görə süzülür
        private static readonly int[] AllTerms = { 6, 12, 18, 24, 36, 48 };

        private readonly ILoanRepository _loans;
        private readonly ICardRepository _cards;
        private readonly IAccountRepository _accounts;
        private readonly ICustomerRepository _customers;
        private readonly IBaseRepository<OwnBank> _ownBanks;
        private readonly IValidator<ApplyLoanDto> _applyValidator;
        private readonly IValidator<PayLoanDto> _payValidator;
        private readonly IValidator<PayByCodeDto> _payByCodeValidator;
        private readonly INotificationService _notifications;

        public LoanService(ILoanRepository loans,
                           ICardRepository cards,
                           IAccountRepository accounts,
                           ICustomerRepository customers,
                           IBaseRepository<OwnBank> ownBanks,
                           IValidator<ApplyLoanDto> applyValidator,
                           IValidator<PayLoanDto> payValidator,
                           IValidator<PayByCodeDto> payByCodeValidator,
                           INotificationService notifications)
        {
            _notifications = notifications;
            _loans = loans;
            _cards = cards;
            _accounts = accounts;
            _customers = customers;
            _ownBanks = ownBanks;
            _applyValidator = applyValidator;
            _payValidator = payValidator;
            _payByCodeValidator = payByCodeValidator;
        }

        private static DateTime Today()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow);
        }

        // ---------- Qaydalar və siyahı ----------

        public async Task<ServiceResult<LoanOptionsDto>> GetOptionsAsync(string userId)
        {
            var bank = await _ownBanks.GetAsync();
            if (bank is null)
            {
                return ServiceResult<LoanOptionsDto>.Fail("Loans are not available right now.");
            }

            var openCount = await _loans.CountOpenAsync(userId);
            var reason = await CannotApplyReasonAsync(userId, bank, openCount);

            return ServiceResult<LoanOptionsDto>.Ok(new LoanOptionsDto
            {
                MinAmount = bank.MinLoanAmount,
                MaxAmount = bank.MaxLoanAmount,
                AnnualInterestRate = bank.AnnualInterestRate,
                TermOptions = TermsFor(bank),
                MaxActiveLoans = bank.MaxActiveLoans,
                OpenLoanCount = openCount,
                CanApply = reason is null,
                CannotApplyReason = reason,
                MinEarlyPayment = bank.MinEarlyPayment,
                EarlyRepaymentMinPercent = bank.EarlyRepaymentMinPercent,
                EarlyRepaymentRateReduction = bank.EarlyRepaymentRateReduction,
                MinAnnualInterestRate = bank.MinAnnualInterestRate
            });
        }

        private static int[] TermsFor(OwnBank bank)
        {
            return AllTerms.Where(t => t >= bank.MinTermMonths && t <= bank.MaxTermMonths).ToArray();
        }

        // Müraciət etməyə mane olan səbəb (yoxdursa null)
        private async Task<string?> CannotApplyReasonAsync(string userId, OwnBank bank, int openCount)
        {
            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return "Your account was not found.";
            }
            if (user.IsRestricted)
            {
                return "Your account is restricted. Please contact support.";
            }
            if (user.CustomerId is null)
            {
                return "Your account is not linked to a citizen record, so a loan can't be requested.";
            }

            var customer = await _customers.GetByIdAsync(user.CustomerId.Value);
            if (customer is not null && LoanEngine.AgeOn(customer.BirthDate, Today()) < bank.MinAge)
            {
                return "You must be at least " + bank.MinAge + " years old to request a loan.";
            }

            if (openCount >= bank.MaxActiveLoans)
            {
                return "You already have " + openCount + " open loans or applications. The limit is " + bank.MaxActiveLoans + ".";
            }

            return null;
        }

        public async Task<IEnumerable<LoanSummaryDto>> GetMyLoansAsync(string userId)
        {
            var loans = await _loans.GetByBorrowerAsync(userId);
            var today = Today();
            return loans.Select(l => LoanMapper.ToSummary(l, today)).ToList();
        }

        public async Task<ServiceResult<LoanDetailDto>> GetMyLoanAsync(string userId, int loanId)
        {
            var loan = await _loans.GetDetailForBorrowerAsync(loanId, userId);
            if (loan is null)
            {
                return ServiceResult<LoanDetailDto>.NotFound();
            }

            return ServiceResult<LoanDetailDto>.Ok(LoanMapper.ToDetail(loan, Today()));
        }

        // ---------- Müraciət ----------

        public async Task<ServiceResult<LoanSummaryDto>> ApplyAsync(string userId, ApplyLoanDto model)
        {
            var validation = await _applyValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanSummaryDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var bank = await _ownBanks.GetAsync();
            if (bank is null)
            {
                return ServiceResult<LoanSummaryDto>.Fail("Loans are not available right now.");
            }

            if (model.Amount < bank.MinLoanAmount || model.Amount > bank.MaxLoanAmount)
            {
                return ServiceResult<LoanSummaryDto>.Fail("The amount must be between " + Money(bank.MinLoanAmount) + " and " + Money(bank.MaxLoanAmount) + " AZN.");
            }
            if (!TermsFor(bank).Contains(model.TermMonths))
            {
                return ServiceResult<LoanSummaryDto>.Fail("Choose one of the available terms.");
            }

            var openCount = await _loans.CountOpenAsync(userId);
            var reason = await CannotApplyReasonAsync(userId, bank, openCount);
            if (reason is not null)
            {
                return ServiceResult<LoanSummaryDto>.Fail(reason);
            }

            var user = await _accounts.GetByIdAsync(userId);
            var customer = await _customers.GetWithHistoryAsync(user!.CustomerId!.Value);
            if (customer is null)
            {
                return ServiceResult<LoanSummaryDto>.Fail("Your citizen record was not found.");
            }

            // Gəlir: hazırda işlədiyi iş yerinin maaşı (bir neçə olarsa ən yenisi)
            var job = customer.Employments
                .Where(e => e.EndDate == null)
                .OrderByDescending(e => e.StartDate)
                .FirstOrDefault();

            // Qara siyahıdakı vətəndaşa iş olmasa belə kredit verilmir: müraciət qeydə alınır və rədd olunur.
            // Başqaları üçün iş yoxdursa gəlir bilinmir, müraciət qəbul olunmur
            if (job is null && !customer.IsBlacklisted)
            {
                return ServiceResult<LoanSummaryDto>.Fail("We couldn't find a current job in your record, so a loan can't be requested.");
            }

            // Pul düşəcək kart: öz kartı, bloklanmamış, Cashback olmayan
            var card = await _cards.GetByIdForUserAsync(model.TargetCardId, userId);
            if (card is null)
            {
                return ServiceResult<LoanSummaryDto>.Fail("Choose one of your own cards to receive the money.");
            }
            if (card.IsBlocked)
            {
                return ServiceResult<LoanSummaryDto>.Fail("That card is blocked. Choose another card.");
            }
            if (card.Tier == CardTier.Cashback)
            {
                return ServiceResult<LoanSummaryDto>.Fail("The money can't be paid to a Cashback card. Choose another card.");
            }

            var purpose = Enum.Parse<LoanPurpose>(model.Purpose, true);
            var quote = LoanEngine.Quote(model.Amount, bank.AnnualInterestRate, model.TermMonths);
            var now = DateTime.UtcNow;

            var loan = new Loan
            {
                OwnBankId = bank.Id,
                BorrowerUserId = userId,
                Purpose = purpose,
                PurposeNote = string.IsNullOrWhiteSpace(model.PurposeNote) ? null : model.PurposeNote.Trim(),
                RequestedAmount = model.Amount,
                TermMonths = model.TermMonths,
                MonthlyPayment = quote.MonthlyPayment,
                TotalRepayment = quote.TotalRepayment,
                IncomeAtApply = job?.MonthlySalary ?? 0m,
                InitialAnnualInterestRate = bank.AnnualInterestRate,
                AnnualInterestRate = bank.AnnualInterestRate,
                FixedPenalty = bank.FixedPenalty,
                DailyPenaltyPercent = bank.DailyPenaltyPercent,
                TargetCardId = card.Id,
                Status = LoanStatus.Pending
            };

            // Qara siyahıdakı vətəndaşa kredit verilmir: müraciət qeydə alınır və dərhal rədd olunur (səbəb daxili qalır)
            if (customer.IsBlacklisted)
            {
                loan.Status = LoanStatus.Rejected;
                loan.RejectReason = "Blacklist: " + (customer.BlacklistReason ?? "no reason recorded");
                loan.ReviewedAt = now;
            }

            var created = await _loans.CreateAsync(loan, now);

            // Baxış gözləyən müraciət pul rolundakı işçilərin zəngində görünür (qara siyahıdan dərhal rədd olunan göstərilmir)
            if (created.Status == LoanStatus.Pending)
            {
                await _notifications.NotifyRolesAsync(
                    new[] { Roles.Accountant, Roles.Admin, Roles.SuperAdmin },
                    NotificationCategory.Staff,
                    "New loan application",
                    $"{user.Name} {user.Surname} asked for {Money(created.RequestedAmount)} AZN over {created.TermMonths} months.",
                    $"/Admin/Loans/Detail/{created.Id}");
            }

            return ServiceResult<LoanSummaryDto>.Ok(LoanMapper.ToSummary(created, LoanEngine.BakuToday(now)));
        }

        private static string Money(decimal value)
        {
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---------- Təklifə cavab ----------

        public async Task<ServiceResult<LoanSummaryDto>> AcceptOfferAsync(string userId, int loanId, AcceptLoanOfferDto model)
        {
            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<LoanSummaryDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<LoanSummaryDto>.Fail("Your account is restricted. Please contact support.");
            }

            var result = await _loans.AcceptOfferAsync(loanId, userId, model.TargetCardId, DateTime.UtcNow);
            return await ToSummaryResultAsync(result, userId);
        }

        public async Task<ServiceResult<LoanSummaryDto>> DeclineOfferAsync(string userId, int loanId)
        {
            var result = await _loans.DeclineOfferAsync(loanId, userId, DateTime.UtcNow);
            return await ToSummaryResultAsync(result, userId);
        }

        private async Task<ServiceResult<LoanSummaryDto>> ToSummaryResultAsync(LoanResult result, string userId)
        {
            if (result.Outcome != LoanOutcome.Success)
            {
                return LoanMessages.ToFailure<LoanSummaryDto>(result);
            }

            var loan = await _loans.GetDetailForBorrowerAsync(result.LoanId, userId);
            if (loan is null)
            {
                return ServiceResult<LoanSummaryDto>.NotFound();
            }
            return ServiceResult<LoanSummaryDto>.Ok(LoanMapper.ToSummary(loan, Today()));
        }

        // ---------- Ödəniş ----------

        public async Task<ServiceResult<LoanReceiptDto>> PayAsync(string userId, int loanId, PayLoanDto model)
        {
            var validation = await _payValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanReceiptDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<LoanReceiptDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<LoanReceiptDto>.Fail("Your account is restricted. Please contact support.");
            }

            var type = Enum.Parse<LoanPaymentType>(model.Type, true);
            var plan = new LoanPaymentPlan(
                PayerUserId: userId,
                LoanId: loanId,
                CardId: model.CardId,
                Type: type,
                Amount: type == LoanPaymentType.FullEarly ? 0m : model.Amount,
                Reference: ReferenceFor(model.RequestId!),
                OwnerOnly: type != LoanPaymentType.Installment,
                UtcNow: DateTime.UtcNow);

            return await ExecutePaymentAsync(plan, userId);
        }

        public async Task<ServiceResult<LoanReceiptDto>> PayByCodeAsync(string userId, PayByCodeDto model)
        {
            var validation = await _payByCodeValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanReceiptDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<LoanReceiptDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<LoanReceiptDto>.Fail("Your account is restricted. Please contact support.");
            }

            var loan = await _loans.GetByContractNumberAsync(model.ContractNumber.Trim().ToUpperInvariant());
            if (loan is null)
            {
                return ServiceResult<LoanReceiptDto>.Fail("We couldn't find a loan with this code.");
            }

            // Kodla yalnız aylıq ödəniş edilir (erkən ödənişlər yalnız sahibinə aiddir)
            var plan = new LoanPaymentPlan(
                PayerUserId: userId,
                LoanId: loan.Id,
                CardId: model.CardId,
                Type: LoanPaymentType.Installment,
                Amount: model.Amount,
                Reference: ReferenceFor(model.RequestId!),
                OwnerOnly: false,
                UtcNow: DateTime.UtcNow);

            return await ExecutePaymentAsync(plan, userId);
        }

        // RequestId-dən kart jurnalında görünən qısa kod (LP-3F9A...). Eyni sorğu həmişə eyni kodu verir
        private static string ReferenceFor(string requestId)
        {
            return "LP-" + Guid.Parse(requestId).ToString("N").Substring(0, 16).ToUpperInvariant();
        }

        private async Task<ServiceResult<LoanReceiptDto>> ExecutePaymentAsync(LoanPaymentPlan plan, string userId)
        {
            var result = await _loans.PayAsync(plan);

            if (result.Outcome == LoanOutcome.Duplicate)
            {
                // Forma iki dəfə göndərilib: əvvəlki ödənişin qəbzi qaytarılır, pul ikinci dəfə çıxmır
                var existing = await _loans.FindPaymentAsync(plan.Reference, userId);
                if (existing is null)
                {
                    return ServiceResult<LoanReceiptDto>.Fail("This payment was already processed.");
                }
                return ServiceResult<LoanReceiptDto>.Ok(LoanMapper.ToReceipt(existing));
            }

            if (result.Outcome != LoanOutcome.Success)
            {
                return LoanMessages.ToFailure<LoanReceiptDto>(result);
            }

            await NotifyOwnerAboutPaymentAsync(plan, result);
            return ServiceResult<LoanReceiptDto>.Ok(LoanMapper.ToReceipt(result));
        }

        // Kredit başqası tərəfindən (ödəniş kodu ilə) ödənibsə və ya bu ödənişlə bağlanıbsa, sahibi bildirişdə görür
        private async Task NotifyOwnerAboutPaymentAsync(LoanPaymentPlan plan, LoanResult result)
        {
            var loan = await _loans.GetDetailAsync(plan.LoanId);
            if (loan is null)
            {
                return;
            }

            if (loan.BorrowerUserId != plan.PayerUserId)
            {
                await _notifications.NotifyAsync(loan.BorrowerUserId, NotificationCategory.Loan, "Payment received",
                    $"A payment of {result.Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} AZN was made on your loan {loan.ContractNumber} from another account.",
                    $"/App/Loans/{loan.Id}");
            }

            if (result.LoanClosed)
            {
                await _notifications.NotifyAsync(loan.BorrowerUserId, NotificationCategory.Loan, "Loan paid off",
                    $"Your loan {loan.ContractNumber} is fully paid. Thank you.", $"/App/Loans/{loan.Id}", "closed:" + loan.Id);
            }
        }

        // ---------- Ödəniş kodu ilə axtarış ----------

        public async Task<ServiceResult<LoanLookupDto>> LookupAsync(string contractNumber)
        {
            var code = (contractNumber ?? string.Empty).Trim().ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^KR-\d{4}-\d{6}$"))
            {
                return ServiceResult<LoanLookupDto>.Fail("Enter the loan code like KR-2026-000123.");
            }

            var loan = await _loans.GetByContractNumberAsync(code);

            // Açıq olmayan kredit üçün "tapılmadı" deyilir: kodun varlığı bilinməsin
            if (loan is null || (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Court))
            {
                return ServiceResult<LoanLookupDto>.NotFound();
            }

            var due = LoanEngine.GetDue(loan, Today());
            return ServiceResult<LoanLookupDto>.Ok(new LoanLookupDto
            {
                ContractNumber = loan.ContractNumber,
                OwnerName = LoanMapper.MaskName(loan.Borrower.Name, loan.Borrower.Surname),
                AnnualInterestRate = loan.AnnualInterestRate,
                DueTotal = due.Total,
                OverdueTotal = due.OverdueTotal,
                NextDueDate = due.Items.Count > 0 ? due.Items[0].Installment.DueDate : null,
                CanPay = due.Total > 0m
            });
        }

        // ---------- Gündəlik iş ----------

        public async Task<int> RunDailyMaintenanceAsync()
        {
            var result = await _loans.RunMaintenanceAsync(DateTime.UtcNow);

            // Müştəriyə deyilməli hadisələr bildirişə çevrilir. Key eyni hadisənin hər saat təkrar yazılmasının qarşısını alır
            foreach (var notice in result.Notices)
            {
                await NotifyAboutAsync(notice);
            }
            return result.Changed;
        }

        private async Task NotifyAboutAsync(LoanNotice notice)
        {
            var link = "/App/Loans/" + notice.LoanId;
            var amount = notice.Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
            var date = notice.DueDate.HasValue ? notice.DueDate.Value.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : string.Empty;

            switch (notice.Kind)
            {
                case LoanNoticeKind.OfferExpired:
                    await _notifications.NotifyAsync(notice.UserId, NotificationCategory.Loan, "Offer expired",
                        $"Your offer for loan {notice.ContractNumber} expired. You can apply again.", link, notice.Key);
                    break;
                case LoanNoticeKind.DueSoon:
                    await _notifications.NotifyAsync(notice.UserId, NotificationCategory.Loan, "Payment due soon",
                        $"Your payment of {amount} AZN for loan {notice.ContractNumber} is due on {date}.", link, notice.Key);
                    break;
                case LoanNoticeKind.Overdue:
                    await _notifications.NotifyAsync(notice.UserId, NotificationCategory.Loan, "Payment overdue",
                        $"Your payment for loan {notice.ContractNumber} was due on {date}. {amount} AZN is due now and a penalty grows every day until you pay.", link, notice.Key);
                    break;
                case LoanNoticeKind.Court:
                    await _notifications.NotifyAsync(notice.UserId, NotificationCategory.Loan, "Loan passed to court",
                        $"Loan {notice.ContractNumber} has been overdue for {notice.Days} days and was passed to court. Please pay what is due as soon as possible.", link, notice.Key);
                    break;
            }
        }
    }
}
