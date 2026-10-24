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
    public class LoanReviewService : ILoanReviewService
    {
        private readonly ILoanRepository _loans;
        private readonly ICustomerRepository _customers;
        private readonly IAccountRepository _accounts;
        private readonly IBaseRepository<OwnBank> _ownBanks;
        private readonly IValidator<LoanReviewFilterDto> _filterValidator;
        private readonly IValidator<ApproveLoanDto> _approveValidator;
        private readonly IValidator<RejectLoanDto> _rejectValidator;
        private readonly INotificationService _notifications;

        public LoanReviewService(ILoanRepository loans,
                                 ICustomerRepository customers,
                                 IAccountRepository accounts,
                                 IBaseRepository<OwnBank> ownBanks,
                                 IValidator<LoanReviewFilterDto> filterValidator,
                                 IValidator<ApproveLoanDto> approveValidator,
                                 IValidator<RejectLoanDto> rejectValidator,
                                 INotificationService notifications)
        {
            _notifications = notifications;
            _loans = loans;
            _customers = customers;
            _accounts = accounts;
            _ownBanks = ownBanks;
            _filterValidator = filterValidator;
            _approveValidator = approveValidator;
            _rejectValidator = rejectValidator;
        }

        private static DateTime Today()
        {
            return LoanEngine.BakuToday(DateTime.UtcNow);
        }

        // ---------- Siyahı ----------

        public async Task<ServiceResult<LoanReviewPageDto>> GetPageAsync(LoanReviewFilterDto filter)
        {
            var validation = await _filterValidator.ValidateAsync(filter);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanReviewPageDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            LoanStatus? status = null;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                status = Enum.Parse<LoanStatus>(filter.Status, true);
            }

            var skip = (filter.Page - 1) * filter.PageSize;
            var page = await _loans.GetReviewPageAsync(status, filter.Search, skip, filter.PageSize);

            // Gözləyən müraciətlərin sayı süzgəcdən asılı olmayaraq göstərilir (işçinin işi)
            var pending = await _loans.GetReviewPageAsync(LoanStatus.Pending, null, 0, 1);
            var awaiting = await _loans.GetReviewPageAsync(LoanStatus.AwaitingSuperAdmin, null, 0, 1);

            var dto = new LoanReviewPageDto
            {
                TotalCount = page.TotalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                PendingCount = pending.TotalCount,
                AwaitingSuperAdminCount = awaiting.TotalCount,
                Items = page.Items.Select(l => new LoanReviewListItemDto
                {
                    Id = l.Id,
                    ContractNumber = l.ContractNumber,
                    ApplicantName = l.Borrower.Name + " " + l.Borrower.Surname,
                    Fin = l.Borrower.FinKod,
                    Purpose = l.Purpose.ToString(),
                    RequestedAmount = l.RequestedAmount,
                    ApprovedAmount = l.ApprovedAmount,
                    TermMonths = l.TermMonths,
                    CreatedAt = l.CreatedAt,
                    Status = l.Status.ToString(),
                    RequiresSuperAdmin = l.RequiresSuperAdminApproval
                }).ToList()
            };

            return ServiceResult<LoanReviewPageDto>.Ok(dto);
        }

        // ---------- Müraciətin tam məlumatı ----------

        public async Task<ServiceResult<LoanReviewDetailDto>> GetDetailAsync(int loanId)
        {
            var loan = await _loans.GetDetailAsync(loanId);
            if (loan is null)
            {
                return ServiceResult<LoanReviewDetailDto>.NotFound();
            }

            var bank = loan.OwnBank;
            var borrower = loan.Borrower;
            var today = Today();

            Customer? customer = null;
            if (borrower.CustomerId.HasValue)
            {
                customer = await _customers.GetWithHistoryAsync(borrower.CustomerId.Value);
            }

            var detail = new LoanReviewDetailDto
            {
                Loan = LoanMapper.ToDetail(loan, today, true),
                RejectReason = loan.RejectReason,
                ReviewedAt = loan.ReviewedAt,
                CanDecide = loan.Status == LoanStatus.Pending || loan.Status == LoanStatus.AwaitingSuperAdmin,
                RequiresSuperAdminApproval = loan.RequiresSuperAdminApproval,
                FirstApprovedAt = loan.FirstApprovedAt,
                FirstApprovedAmount = loan.FirstApprovedAmount
            };

            // İki təsdiqli (qara siyahı) kreditdə hansı mərhələdə olduğu: əvvəl kredit şöbəsi, sonra Super Admin
            if (loan.Status == LoanStatus.AwaitingSuperAdmin)
            {
                detail.Stage = "FinalApproval";
            }
            else if (loan.Status == LoanStatus.Pending && loan.RequiresSuperAdminApproval)
            {
                detail.Stage = "FirstApproval";
            }

            if (!string.IsNullOrEmpty(loan.FirstApprovedByUserId))
            {
                var first = await _accounts.GetByIdAsync(loan.FirstApprovedByUserId);
                detail.FirstApprovedBy = first is null ? null : first.Name + " " + first.Surname;
            }

            if (!string.IsNullOrEmpty(loan.ReviewedByUserId))
            {
                var reviewer = await _accounts.GetByIdAsync(loan.ReviewedByUserId);
                detail.ReviewedBy = reviewer is null ? null : reviewer.Name + " " + reviewer.Surname;
            }

            detail.Applicant = new LoanApplicantDto
            {
                UserId = borrower.Id,
                Name = customer?.Name ?? borrower.Name,
                Surname = customer?.Surname ?? borrower.Surname,
                FatherName = customer?.FatherName,
                Fin = customer?.Fin ?? borrower.FinKod,
                BirthDate = customer?.BirthDate ?? borrower.BirthDay,
                Age = LoanEngine.AgeOn(customer?.BirthDate ?? borrower.BirthDay, today),
                ResidenceAddress = customer?.ResidenceAddress,
                PhotoUrl = customer?.PhotoUrl,
                IsBlacklisted = customer?.IsBlacklisted ?? false,
                BlacklistReason = customer?.BlacklistReason,
                Email = borrower.Email,
                PhoneNumber = borrower.PhoneNumber,
                AccountRestricted = borrower.IsRestricted
            };

            // Müraciətdən sonra qara siyahıya düşübsə də iki təsdiq tələb olunur
            if (loan.Status == LoanStatus.Pending && detail.Applicant.IsBlacklisted)
            {
                detail.RequiresSuperAdminApproval = true;
                detail.Stage = "FirstApproval";
            }

            if (customer is not null)
            {
                detail.Employments = customer.Employments
                    .OrderByDescending(e => e.StartDate)
                    .Select(e => new ApplicantEmploymentDto
                    {
                        Workplace = e.Workplace.Name,
                        Position = e.Position,
                        MonthlySalary = e.MonthlySalary,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        LeaveReason = e.LeaveReason,
                        IsCurrent = e.EndDate == null
                    }).ToList();

                detail.ExternalLoans = customer.ExternalLoans
                    .OrderBy(l => l.IsClosed)
                    .ThenByDescending(l => l.StartDate)
                    .Select(l => new ApplicantExternalLoanDto
                    {
                        Bank = l.ExternalBank.Name,
                        Purpose = l.Purpose.ToString(),
                        BorrowedAmount = l.BorrowedAmount,
                        PaidAmount = l.PaidAmount,
                        RemainingAmount = l.IsClosed ? 0m : Math.Max(0m, l.BorrowedAmount - l.PaidAmount),
                        MonthlyPayment = l.MonthlyPayment,
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        IsClosed = l.IsClosed,
                        DelayCount = l.DelayCount,
                        MaxDelayDays = l.MaxDelayDays,
                        OverdueAmount = l.OverdueAmount,
                        WentToCourt = l.WentToCourt
                    }).ToList();
            }

            detail.ExternalTotals = new ExternalLoanTotalsDto
            {
                TotalBorrowed = detail.ExternalLoans.Sum(l => l.BorrowedAmount),
                TotalPaid = detail.ExternalLoans.Sum(l => l.PaidAmount),
                TotalRemaining = detail.ExternalLoans.Sum(l => l.RemainingAmount),
                TotalOverdue = detail.ExternalLoans.Sum(l => l.OverdueAmount),
                OpenCount = detail.ExternalLoans.Count(l => !l.IsClosed),
                TotalDelays = detail.ExternalLoans.Sum(l => l.DelayCount),
                CourtCount = detail.ExternalLoans.Count(l => l.WentToCourt),
                OpenMonthlyPayments = detail.ExternalLoans.Where(l => !l.IsClosed).Sum(l => l.MonthlyPayment)
            };

            // Caspian-dakı digər kreditləri
            var others = (await _loans.GetAllByBorrowerWithInstallmentsAsync(borrower.Id))
                .Where(l => l.Id != loan.Id)
                .ToList();
            foreach (var other in others)
            {
                var overdueCount = 0;
                var overdueAmount = 0m;
                if (other.Status == LoanStatus.Active || other.Status == LoanStatus.Court)
                {
                    var due = LoanEngine.GetDue(other, today);
                    overdueCount = due.OverdueCount;
                    overdueAmount = due.OverdueTotal;
                }

                detail.CaspianLoans.Add(new ApplicantCaspianLoanDto
                {
                    Id = other.Id,
                    ContractNumber = other.ContractNumber,
                    Status = other.Status.ToString(),
                    Amount = other.ApprovedAmount ?? other.RequestedAmount,
                    OutstandingPrincipal = other.OutstandingPrincipal,
                    MonthlyPayment = other.MonthlyPayment,
                    OverdueInstallments = overdueCount,
                    OverdueAmount = overdueAmount,
                    CreatedAt = other.CreatedAt,
                    ClosedEarly = other.ClosedEarly
                });
            }

            detail.Rules = await BuildRulesAsync(loan, bank, detail, others);
            return ServiceResult<LoanReviewDetailDto>.Ok(detail);
        }

        // Qaydalara görə rəqəmlər və xəbərdarlıqlar: işçinin qərarına kömək edir, qərarı özü vermir
        private async Task<LoanReviewRulesDto> BuildRulesAsync(Loan loan, OwnBank bank, LoanReviewDetailDto detail, List<Loan> others)
        {
            var currentJob = detail.Employments.FirstOrDefault(e => e.IsCurrent);
            var income = currentJob?.MonthlySalary ?? 0m;

            var delays = detail.ExternalTotals.TotalDelays;
            var existingMonthly = detail.ExternalTotals.OpenMonthlyPayments + await _loans.SumActiveMonthlyPaymentsAsync(loan.BorrowerUserId);

            var maxByIncome = LoanEngine.MaxByIncome(income, delays, bank.MaxLoanAmount);

            decimal? maxByLimit = null;
            if (bank.MaxPaymentToIncomePercent.HasValue)
            {
                maxByLimit = LoanEngine.MaxByPaymentLimit(income, bank.MaxPaymentToIncomePercent.Value, existingMonthly, loan.AnnualInterestRate, loan.TermMonths);
            }

            var suggestedMax = maxByLimit.HasValue ? Math.Min(maxByIncome, maxByLimit.Value) : maxByIncome;
            var suggestedApproval = Math.Floor(Math.Min(loan.RequestedAmount, suggestedMax));
            if (suggestedApproval < bank.MinLoanAmount)
            {
                suggestedApproval = 0m;
            }

            var requestedMonthly = LoanEngine.MonthlyPayment(loan.RequestedAmount, loan.AnnualInterestRate, loan.TermMonths);
            var percent = income > 0m ? LoanEngine.Round((existingMonthly + requestedMonthly) / income * 100m) : 0m;
            var activeCount = others.Count(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Court);

            var rules = new LoanReviewRulesDto
            {
                CurrentIncome = income,
                DelayMultiplier = LoanEngine.DelayMultiplier(delays),
                MaxByIncome = maxByIncome,
                MaxByPaymentLimit = maxByLimit,
                SuggestedMax = suggestedMax,
                SuggestedApproval = suggestedApproval,
                ExistingMonthlyPayments = existingMonthly,
                PaymentToIncomePercent = percent,
                PaymentLimitPercent = bank.MaxPaymentToIncomePercent,
                ActiveLoanCount = activeCount,
                MaxActiveLoans = bank.MaxActiveLoans,
                MinAmount = bank.MinLoanAmount,
                MaxAmount = bank.MaxLoanAmount,
                RequestedMonthlyPayment = requestedMonthly
            };

            if (detail.Applicant.IsBlacklisted)
            {
                rules.Warnings.Add("The customer is on the blacklist: " + (detail.Applicant.BlacklistReason ?? "no reason recorded") + ". The loan needs two approvals: first the loan staff, then the Super Admin.");
            }
            if (detail.Applicant.AccountRestricted)
            {
                rules.Warnings.Add("The customer's account is restricted.");
            }
            if (currentJob is null)
            {
                rules.Warnings.Add("There is no current job in the record, so the income is unknown.");
            }
            if (detail.Applicant.Age < bank.MinAge)
            {
                rules.Warnings.Add("The customer is younger than " + bank.MinAge + ".");
            }
            if (delays > 0)
            {
                rules.Warnings.Add(delays + " delayed payment(s) at other banks: the income multiplier is " + rules.DelayMultiplier + ".");
            }
            if (detail.ExternalTotals.TotalOverdue > 0m)
            {
                rules.Warnings.Add("Overdue amount at other banks: " + LoanEngine.Round(detail.ExternalTotals.TotalOverdue).ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN.");
            }
            if (detail.ExternalTotals.CourtCount > 0)
            {
                rules.Warnings.Add(detail.ExternalTotals.CourtCount + " loan(s) at other banks went to court.");
            }
            if (detail.CaspianLoans.Any(l => l.OverdueInstallments > 0))
            {
                rules.Warnings.Add("The customer has overdue installments on another Caspian loan.");
            }
            if (loan.RequestedAmount > suggestedMax)
            {
                rules.Warnings.Add(suggestedMax <= 0m
                    ? "Based on the income and the payments the customer already has, no new loan amount fits the monthly payment limit."
                    : "The requested amount is above the suggested maximum of " + suggestedMax.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " AZN.");
            }
            if (bank.MaxPaymentToIncomePercent.HasValue && percent > bank.MaxPaymentToIncomePercent.Value)
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                rules.Warnings.Add("Existing payments " + LoanEngine.Round(existingMonthly).ToString("N2", inv)
                    + " AZN + this loan " + LoanEngine.Round(requestedMonthly).ToString("N2", inv)
                    + " AZN a month would be " + percent.ToString("0.##", inv) + "% of the income (limit "
                    + bank.MaxPaymentToIncomePercent.Value.ToString("0.##", inv) + "%).");
            }
            if (activeCount >= bank.MaxActiveLoans)
            {
                rules.Warnings.Add("The customer already has " + activeCount + " active loans (limit " + bank.MaxActiveLoans + ").");
            }

            return rules;
        }

        // ---------- Qərar ----------

        public async Task<ServiceResult<LoanSummaryDto>> ApproveAsync(string staffUserId, int loanId, ApproveLoanDto model)
        {
            var validation = await _approveValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanSummaryDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var loan = await _loans.GetDetailAsync(loanId);
            if (loan is null)
            {
                return ServiceResult<LoanSummaryDto>.NotFound();
            }
            if (loan.Status != LoanStatus.Pending && loan.Status != LoanStatus.AwaitingSuperAdmin)
            {
                return ServiceResult<LoanSummaryDto>.Fail("This application has already been reviewed.");
            }

            // Qara siyahıdakı müştəri: iki təsdiq lazımdır. Əvvəl kredit şöbəsi (Accountant/Admin), sonra Super Admin
            var blacklisted = loan.RequiresSuperAdminApproval;
            if (!blacklisted && loan.Borrower.CustomerId.HasValue)
            {
                var customer = await _customers.GetByIdAsync(loan.Borrower.CustomerId.Value);
                blacklisted = customer is not null && customer.IsBlacklisted;
            }

            var staff = await _accounts.GetByIdAsync(staffUserId);
            var roles = staff is null ? new List<string>() : (await _accounts.GetRolesAsync(staff)).ToList();
            var isSuperAdmin = roles.Contains(Domain.Constants.Roles.SuperAdmin);
            var isLoanStaff = roles.Contains(Domain.Constants.Roles.Accountant) || roles.Contains(Domain.Constants.Roles.Admin);

            var approvedAmount = model.ApprovedAmount;
            if (loan.Status == LoanStatus.AwaitingSuperAdmin)
            {
                if (!isSuperAdmin)
                {
                    return ServiceResult<LoanSummaryDto>.Fail("Only the Super Admin can give the final approval for a customer on the blacklist.");
                }
                // Super Admin kredit şöbəsinin təsdiq etdiyi məbləği təsdiq edir
                approvedAmount = loan.FirstApprovedAmount ?? model.ApprovedAmount;
            }
            else if (blacklisted && !isLoanStaff)
            {
                return ServiceResult<LoanSummaryDto>.Fail("A loan for a customer on the blacklist must first be approved by the loan staff. The Super Admin gives the final approval after that.");
            }

            var bank = loan.OwnBank;
            if (approvedAmount > loan.RequestedAmount)
            {
                return ServiceResult<LoanSummaryDto>.Fail("The approved amount can't be higher than the requested amount.");
            }
            if (approvedAmount < bank.MinLoanAmount || approvedAmount > bank.MaxLoanAmount)
            {
                return ServiceResult<LoanSummaryDto>.Fail("The approved amount must be between " + bank.MinLoanAmount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                    + " and " + bank.MaxLoanAmount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " AZN.");
            }

            // Eyni anda ən çox MaxActiveLoans aktiv kredit
            var others = await _loans.GetAllByBorrowerWithInstallmentsAsync(loan.BorrowerUserId);
            var activeCount = others.Count(l => l.Id != loan.Id && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Court));
            if (activeCount >= bank.MaxActiveLoans)
            {
                return ServiceResult<LoanSummaryDto>.Fail("The customer already has " + activeCount + " active loans. The limit is " + bank.MaxActiveLoans + ".");
            }

            // Qara siyahı, birinci təsdiq: pul getmir, sorğu Super Admin-ə düşür
            if (blacklisted && loan.Status == LoanStatus.Pending)
            {
                var firstResult = await _loans.FirstApproveAsync(loanId, staffUserId, approvedAmount, DateTime.UtcNow);
                var firstSummary = await ToSummaryResultAsync(firstResult);
                if (firstSummary.IsSuccess)
                {
                    var staffName = staff is null ? "The loan staff" : staff.Name + " " + staff.Surname;
                    var applicant = loan.Borrower.Name + " " + loan.Borrower.Surname;
                    await _notifications.NotifyRolesAsync(
                        new[] { Domain.Constants.Roles.SuperAdmin },
                        NotificationCategory.Staff,
                        "Loan for a blacklisted customer needs your approval",
                        $"{staffName} approved {approvedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} AZN for {applicant} (loan {loan.ContractNumber}), who is on the blacklist. Do you approve it too?",
                        "/Admin/Loans/Detail/" + loan.Id);
                }
                return firstSummary;
            }

            var result = await _loans.ApproveAsync(loanId, staffUserId, approvedAmount, DateTime.UtcNow);
            var summary = await ToSummaryResultAsync(result);

            // Müştəri qərarı dərhal görür: tam məbləğdə pul karta düşüb, azdırsa cavab verməlidir
            if (summary.IsSuccess && summary.Data is not null)
            {
                var link = "/App/Loans/" + loan.Id;
                var amount = approvedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
                if (summary.Data.Status == LoanStatus.Offered.ToString())
                {
                    var asked = loan.RequestedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
                    await _notifications.NotifyAsync(loan.BorrowerUserId, NotificationCategory.Loan, "We have an offer for you",
                        $"We can approve {amount} AZN instead of {asked} AZN for loan {loan.ContractNumber}. Open the loan to accept or decline.", link);
                }
                else if (summary.Data.Status == LoanStatus.Active.ToString())
                {
                    await _notifications.NotifyAsync(loan.BorrowerUserId, NotificationCategory.Loan, "Your loan was approved",
                        $"{amount} AZN for loan {loan.ContractNumber} was sent to your card.", link);
                }
            }

            return summary;
        }

        public async Task<ServiceResult<LoanSummaryDto>> RejectAsync(string staffUserId, int loanId, RejectLoanDto model)
        {
            var validation = await _rejectValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<LoanSummaryDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            // Kredit şöbəsi təsdiq edib Super Admin-ə göndərəndən sonra rədd qərarı yalnız Super Admin-indir
            var current = await _loans.GetDetailAsync(loanId);
            if (current is not null && current.Status == LoanStatus.AwaitingSuperAdmin)
            {
                var staff = await _accounts.GetByIdAsync(staffUserId);
                var roles = staff is null ? new List<string>() : (await _accounts.GetRolesAsync(staff)).ToList();
                if (!roles.Contains(Domain.Constants.Roles.SuperAdmin))
                {
                    return ServiceResult<LoanSummaryDto>.Fail("Only the Super Admin can decide on this application now.");
                }
            }

            var result = await _loans.RejectAsync(loanId, staffUserId, model.Reason.Trim(), DateTime.UtcNow);
            var summary = await ToSummaryResultAsync(result);

            // Müştəri yalnız ümumi mətn görür; rədd səbəbi daxilidir
            if (summary.IsSuccess)
            {
                var loan = await _loans.GetDetailAsync(loanId);
                if (loan is not null)
                {
                    await _notifications.NotifyAsync(loan.BorrowerUserId, NotificationCategory.Loan, "Application not approved",
                        "We can't approve your loan application right now. You can apply again later.", "/App/Loans/" + loan.Id);
                }
            }

            return summary;
        }

        private async Task<ServiceResult<LoanSummaryDto>> ToSummaryResultAsync(LoanResult result)
        {
            if (result.Outcome != LoanOutcome.Success)
            {
                return LoanMessages.ToFailure<LoanSummaryDto>(result);
            }

            var loan = await _loans.GetDetailAsync(result.LoanId);
            if (loan is null)
            {
                return ServiceResult<LoanSummaryDto>.NotFound();
            }
            return ServiceResult<LoanSummaryDto>.Ok(LoanMapper.ToSummary(loan, Today(), true));
        }
    }
}
