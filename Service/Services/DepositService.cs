using Domain.Deposits;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Repository.Results;
using Service.Helpers.DTOs.Deposits;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class DepositService : IDepositService
    {
        private const int MaxPageSize = 100;

        private readonly IDepositRepository _deposits;
        private readonly IAccountRepository _accounts;
        private readonly INotificationService _notifications;
        private readonly IValidator<OpenDepositDto> _openValidator;
        private readonly IValidator<UpdateDepositPlanDto> _planValidator;

        public DepositService(IDepositRepository deposits,
                              IAccountRepository accounts,
                              INotificationService notifications,
                              IValidator<OpenDepositDto> openValidator,
                              IValidator<UpdateDepositPlanDto> planValidator)
        {
            _deposits = deposits;
            _accounts = accounts;
            _notifications = notifications;
            _openValidator = openValidator;
            _planValidator = planValidator;
        }

        // ---------- Müştəri ----------

        public async Task<DepositsOverviewDto> GetOverviewAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var plans = await _deposits.GetPlansAsync(true);
            var deposits = await _deposits.GetByUserAsync(userId);
            var items = deposits.Select(d => ToDto(d, now)).ToList();
            var active = items.Where(d => d.Status == nameof(DepositStatus.Active)).ToList();

            return new DepositsOverviewDto
            {
                Plans = plans.Select(ToPlanDto).ToList(),
                Deposits = items,
                ActivePrincipal = active.Sum(d => d.Principal),
                ActiveExpectedInterest = active.Sum(d => d.ExpectedInterest),
                InterestEarned = items.Where(d => d.Status == nameof(DepositStatus.Paid)).Sum(d => d.ExpectedInterest)
            };
        }

        public async Task<ServiceResult<DepositDto>> OpenAsync(string userId, OpenDepositDto model)
        {
            var validation = await _openValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<DepositDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accounts.GetByIdAsync(userId);
            if (user is null)
            {
                return ServiceResult<DepositDto>.NotFound();
            }
            if (user.IsRestricted)
            {
                return ServiceResult<DepositDto>.Fail("Your account is restricted. Please contact support.");
            }

            var plan = new DepositOpenPlan(
                UserId: userId,
                PlanId: model.PlanId,
                Amount: decimal.Round(model.Amount, 2),
                SourceCardId: model.SourceCardId,
                PayoutCardId: model.PayoutCardId,
                Reference: ReferenceFor(model.RequestId!),
                UtcNow: DateTime.UtcNow);

            var result = await _deposits.OpenAsync(plan);

            if (result.Outcome == DepositOutcome.Duplicate)
            {
                // Forma iki dəfə göndərilib: əvvəlki depozit qaytarılır, pul ikinci dəfə çıxmır
                var existing = await _deposits.FindByReferenceAsync(plan.Reference, userId);
                if (existing is null)
                {
                    return ServiceResult<DepositDto>.Fail("This deposit was already processed.");
                }
                return ServiceResult<DepositDto>.Ok(ToDto(existing, DateTime.UtcNow));
            }

            if (result.Outcome != DepositOutcome.Success)
            {
                return ToFailure(result.Outcome, plan);
            }

            var opened = await _deposits.FindByReferenceAsync(plan.Reference, userId);
            var dto = ToDto(opened!, DateTime.UtcNow);

            await _notifications.NotifyAsync(userId, NotificationCategory.Deposit, "Deposit opened",
                Money(dto.Principal) + " is locked for " + dto.TermMonths + " months at " + Rate(dto.AnnualRate) + "% a year. You will get "
                + Money(dto.TotalAtMaturity) + " on " + dto.MaturesAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + ".",
                "/App/Deposits", "deposit-open:" + dto.Reference);

            return ServiceResult<DepositDto>.Ok(dto);
        }

        private ServiceResult<DepositDto> ToFailure(DepositOutcome outcome, DepositOpenPlan plan)
        {
            if (outcome == DepositOutcome.PlanNotFound || outcome == DepositOutcome.PlanInactive)
            {
                return ServiceResult<DepositDto>.Fail("This deposit term isn't available right now.");
            }
            if (outcome == DepositOutcome.AmountOutOfRange)
            {
                return ServiceResult<DepositDto>.Fail("The amount is outside the limits of this deposit term.");
            }
            if (outcome == DepositOutcome.SourceNotFound)
            {
                return ServiceResult<DepositDto>.Fail("We couldn't find the card you chose to pay from.");
            }
            if (outcome == DepositOutcome.SourceBlocked)
            {
                return ServiceResult<DepositDto>.Fail("The card you chose to pay from is blocked.");
            }
            if (outcome == DepositOutcome.PayoutNotAllowed)
            {
                return ServiceResult<DepositDto>.Fail("The money must come back to one of your active cards (not the Cashback card).");
            }
            if (outcome == DepositOutcome.InsufficientFunds)
            {
                return ServiceResult<DepositDto>.Fail("There is not enough money on this card.");
            }
            return ServiceResult<DepositDto>.Fail("We couldn't open the deposit right now. Please try again.");
        }

        // ---------- Admin ----------

        public async Task<ServiceResult<DepositPageDto>> GetPageAsync(DepositFilterDto filter)
        {
            DepositStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!Enum.TryParse<DepositStatus>(filter.Status, true, out var parsed) || !Enum.IsDefined(parsed))
                {
                    return ServiceResult<DepositPageDto>.Fail("Unknown deposit status.");
                }
                status = parsed;
            }

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, MaxPageSize);
            var (items, total) = await _deposits.GetPagedAsync(filter.Search, status, page, pageSize);
            var totals = await _deposits.GetTotalsAsync();
            var plans = await _deposits.GetPlansAsync(false);

            return ServiceResult<DepositPageDto>.Ok(new DepositPageDto
            {
                Items = items.Select(d => new AdminDepositDto
                {
                    Reference = d.Reference,
                    CustomerName = (d.User.Name + " " + d.User.Surname).Trim(),
                    Email = d.User.Email ?? string.Empty,
                    TermMonths = d.TermMonths,
                    AnnualRate = d.AnnualRate,
                    Principal = d.Principal,
                    ExpectedInterest = d.ExpectedInterest,
                    StartedAt = d.StartedAt,
                    MaturesAt = d.MaturesAt,
                    Status = d.Status.ToString(),
                    PaidAt = d.PaidAt
                }).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                ActiveCount = totals.ActiveCount,
                ActivePrincipal = totals.ActivePrincipal,
                ExpectedInterest = totals.ExpectedInterest,
                PaidInterest = totals.PaidInterest,
                Plans = plans.Select(ToPlanDto).ToList()
            });
        }

        public async Task<ServiceResult<DepositPlanDto>> UpdatePlanAsync(int id, UpdateDepositPlanDto model)
        {
            var validation = await _planValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<DepositPlanDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var plan = await _deposits.GetPlanAsync(id);
            if (plan is null)
            {
                return ServiceResult<DepositPlanDto>.NotFound();
            }

            plan.AnnualRate = model.AnnualRate;
            plan.MinAmount = model.MinAmount;
            plan.MaxAmount = model.MaxAmount;
            plan.IsActive = model.IsActive;
            await _deposits.UpdatePlanAsync(plan);
            return ServiceResult<DepositPlanDto>.Ok(ToPlanDto(plan));
        }

        // ---------- Gündəlik iş ----------

        public async Task<int> RunMaturityAsync()
        {
            // Əvvəl hər ayın faizi yazılır (müddəti bitməyən depozitlərdə də), sonra müddəti bitənlər ödənilir
            await _deposits.PostDueInterestAsync(DateTime.UtcNow);
            var paid = await _deposits.PayMaturedAsync(DateTime.UtcNow);
            foreach (var item in paid)
            {
                await _notifications.NotifyAsync(item.UserId, NotificationCategory.Deposit, "Deposit matured",
                    Money(item.Principal + item.Interest) + " (" + Money(item.Principal) + " + " + Money(item.Interest)
                    + " interest) was returned to your card •••• " + item.CardLast4 + ".",
                    "/App/Deposits", "deposit-paid:" + item.Reference);
            }
            return paid.Count;
        }

        // ---------- köməkçilər ----------

        private static DepositPlanDto ToPlanDto(DepositPlan plan)
        {
            return new DepositPlanDto
            {
                Id = plan.Id,
                TermMonths = plan.TermMonths,
                AnnualRate = plan.AnnualRate,
                MinAmount = plan.MinAmount,
                MaxAmount = plan.MaxAmount,
                IsActive = plan.IsActive
            };
        }

        private static DepositDto ToDto(Deposit deposit, DateTime nowUtc)
        {
            var paid = deposit.Status == DepositStatus.Paid;
            var progress = paid ? 1m : DepositRules.Progress(deposit.StartedAt, deposit.MaturesAt, nowUtc);
            var daysLeft = paid ? 0 : Math.Max(0, (int)Math.Ceiling((deposit.MaturesAt - nowUtc).TotalDays));

            return new DepositDto
            {
                Reference = deposit.Reference,
                TermMonths = deposit.TermMonths,
                AnnualRate = deposit.AnnualRate,
                Principal = deposit.Principal,
                ExpectedInterest = deposit.ExpectedInterest,
                AccruedInterest = paid ? deposit.ExpectedInterest : deposit.Postings.Sum(p => p.Amount),
                Postings = deposit.Postings.OrderBy(p => p.Number).Select(p => new DepositPostingDto { Number = p.Number, Amount = p.Amount, PostedAt = p.CreatedAt }).ToList(),
                TotalAtMaturity = deposit.Principal + deposit.ExpectedInterest,
                ProgressPercent = (int)Math.Round(progress * 100m),
                DaysLeft = daysLeft,
                StartedAt = deposit.StartedAt,
                MaturesAt = deposit.MaturesAt,
                Status = deposit.Status.ToString(),
                PaidAt = deposit.PaidAt,
                SourceCardLabel = CardLabel(deposit.SourceCard),
                PayoutCardLabel = CardLabel(deposit.PayoutCard)
            };
        }

        private static string CardLabel(Card card)
        {
            return card.Tier + " •••• " + card.CardNumber.Substring(card.CardNumber.Length - 4);
        }

        // RequestId-dən kart jurnalında görünən qısa kod (DP-3F9A...). Eyni sorğu həmişə eyni kodu verir
        private static string ReferenceFor(string requestId)
        {
            return "DP-" + Guid.Parse(requestId).ToString("N").Substring(0, 16).ToUpperInvariant();
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture) + " AZN";
        }

        private static string Rate(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
