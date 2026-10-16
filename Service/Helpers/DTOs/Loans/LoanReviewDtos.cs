namespace Service.Helpers.DTOs.Loans
{
    // İşçinin kredit siyahısı: süzgəc
    public class LoanReviewFilterDto
    {
        public string? Status { get; set; }             // boşdursa hamısı
        public string? Search { get; set; }             // ad, soyad, FİN və ya ödəniş kodu
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class LoanReviewListItemDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string? Fin { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public int TermMonths { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LoanReviewPageDto
    {
        public List<LoanReviewListItemDto> Items { get; set; } = new List<LoanReviewListItemDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PendingCount { get; set; }
    }

    // Müraciətçinin işçiyə göstərilən məlumatı (reyestr + hesab)
    public class LoanApplicantDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? Fin { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public string? ResidenceAddress { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsBlacklisted { get; set; }
        public string? BlacklistReason { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool AccountRestricted { get; set; }
    }

    public class ApplicantEmploymentDto
    {
        public string Workplace { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal MonthlySalary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? LeaveReason { get; set; }
        public bool IsCurrent { get; set; }
    }

    // Başqa bankdakı kredit: işçi nə qədər götürülüb, nə qədər ödənilib, nə qədər gecikib, məhkəməyə düşübmü görür
    public class ApplicantExternalLoanDto
    {
        public string Bank { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal BorrowedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsClosed { get; set; }
        public int DelayCount { get; set; }
        public int MaxDelayDays { get; set; }
        public decimal OverdueAmount { get; set; }
        public bool WentToCourt { get; set; }
    }

    public class ExternalLoanTotalsDto
    {
        public decimal TotalBorrowed { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public decimal TotalOverdue { get; set; }
        public int OpenCount { get; set; }
        public int TotalDelays { get; set; }
        public int CourtCount { get; set; }
        public decimal OpenMonthlyPayments { get; set; }
    }

    // Müştərinin Caspian-dakı digər kreditləri
    public class ApplicantCaspianLoanDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal MonthlyPayment { get; set; }
        public int OverdueInstallments { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool ClosedEarly { get; set; }
    }

    // Qaydalara görə hesablanmış rəqəmlər: işçinin qərarına kömək edir (məcbur etmir)
    public class LoanReviewRulesDto
    {
        public decimal CurrentIncome { get; set; }
        public int DelayMultiplier { get; set; }
        public decimal MaxByIncome { get; set; }
        public decimal? MaxByPaymentLimit { get; set; }
        public decimal SuggestedMax { get; set; }       // yuxarıdakı iki həddin kiçiyi
        public decimal SuggestedApproval { get; set; }  // min(istənilən, SuggestedMax): forma bununla dolur
        public decimal ExistingMonthlyPayments { get; set; }
        public decimal PaymentToIncomePercent { get; set; }   // istənilən məbləğ təsdiqlənsə aylıq ödənişlərin maaşa nisbəti
        public decimal? PaymentLimitPercent { get; set; }
        public int ActiveLoanCount { get; set; }
        public int MaxActiveLoans { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal RequestedMonthlyPayment { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public class LoanReviewDetailDto
    {
        public LoanDetailDto Loan { get; set; } = new LoanDetailDto();
        public LoanApplicantDto Applicant { get; set; } = new LoanApplicantDto();
        public List<ApplicantEmploymentDto> Employments { get; set; } = new List<ApplicantEmploymentDto>();
        public List<ApplicantExternalLoanDto> ExternalLoans { get; set; } = new List<ApplicantExternalLoanDto>();
        public ExternalLoanTotalsDto ExternalTotals { get; set; } = new ExternalLoanTotalsDto();
        public List<ApplicantCaspianLoanDto> CaspianLoans { get; set; } = new List<ApplicantCaspianLoanDto>();
        public LoanReviewRulesDto Rules { get; set; } = new LoanReviewRulesDto();
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectReason { get; set; }
        public bool CanDecide { get; set; }             // Pending olanda true
    }

    public class ApproveLoanDto
    {
        public decimal ApprovedAmount { get; set; }
    }

    public class RejectLoanDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
