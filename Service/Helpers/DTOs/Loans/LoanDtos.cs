namespace Service.Helpers.DTOs.Loans
{
    // Müraciət formasının qaydaları (məbləğ aralığı, müddətlər, faiz) və müştərinin müraciət edə bilib bilməməsi
    public class LoanOptionsDto
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int[] TermOptions { get; set; } = Array.Empty<int>();
        public int MaxActiveLoans { get; set; }
        public int OpenLoanCount { get; set; }
        public bool CanApply { get; set; }
        public string? CannotApplyReason { get; set; }

        // Vaxtından əvvəl ödəniş qaydaları (kredit səhifəsində izah üçün)
        public decimal MinEarlyPayment { get; set; }
        public decimal EarlyRepaymentMinPercent { get; set; }
        public decimal EarlyRepaymentRateReduction { get; set; }
        public decimal MinAnnualInterestRate { get; set; }
    }

    public class ApplyLoanDto
    {
        public decimal Amount { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string? PurposeNote { get; set; }        // Purpose = Other olanda məcburidir
        public int TermMonths { get; set; }
        public int TargetCardId { get; set; }           // pul düşəcək öz kartı (Cashback olmaz)
    }

    // Siyahıdakı bir kredit
    public class LoanSummaryDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DisbursedAt { get; set; }
        public DateTime? OfferExpiresAt { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal NextDueAmount { get; set; }
        public bool HasOverdue { get; set; }
        public string? Message { get; set; }            // müştəriyə göstərilən izah (rədd, imtina, vaxtı bitmə)
    }

    // İndi ödənilməli məbləğ (gecikmiş aylar + növbəti ay)
    public class LoanDueDto
    {
        public decimal Total { get; set; }
        public decimal Penalty { get; set; }
        public decimal Interest { get; set; }
        public decimal Principal { get; set; }
        public decimal OverdueTotal { get; set; }
        public int OverdueCount { get; set; }
        public int InstallmentCount { get; set; }
        public DateTime? NextDueDate { get; set; }
    }

    public class LoanInstallmentDto
    {
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public decimal Payment { get; set; }            // Principal + Interest
        public decimal Penalty { get; set; }            // cərimənin cəmi (sabit + gündəlik)
        public decimal Paid { get; set; }               // ödənilən (cərimə daxil)
        public decimal Remaining { get; set; }          // hələ ödənilməli (cərimə daxil)
        public string Status { get; set; } = string.Empty;   // Paid, Prepaid, Upcoming, Overdue
        public int DaysOverdue { get; set; }
    }

    public class LoanPaymentDto
    {
        public DateTime PaidAt { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Penalty { get; set; }
        public decimal Interest { get; set; }
        public decimal Principal { get; set; }
        public decimal? InterestRateAfter { get; set; }
        public string CardLabel { get; set; } = string.Empty;   // •••• 1234
        public string Reference { get; set; } = string.Empty;
    }

    // Kreditin səhifəsi: xülasə + cədvəl + ödənişlər + ödəniş imkanları
    public class LoanDetailDto : LoanSummaryDto
    {
        public string? PurposeNote { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal InitialAnnualInterestRate { get; set; }
        public string TargetCardLabel { get; set; } = string.Empty;
        public int TargetCardId { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool ClosedEarly { get; set; }
        public decimal FixedPenalty { get; set; }
        public decimal DailyPenaltyPercent { get; set; }

        public List<LoanInstallmentDto> Installments { get; set; } = new List<LoanInstallmentDto>();
        public List<LoanPaymentDto> Payments { get; set; } = new List<LoanPaymentDto>();

        public LoanDueDto Due { get; set; } = new LoanDueDto();
        public decimal PayoffAmount { get; set; }
        public bool CanPayInstallment { get; set; }
        public bool CanPartialEarly { get; set; }       // vaxtından əvvəl hissəvi ödəniş (faiz azala bilər)
        public bool CanPayOff { get; set; }             // krediti vaxtından əvvəl tam bağlamaq
        public string? EarlyBlockedReason { get; set; }

        public decimal MinEarlyPayment { get; set; }
        public decimal EarlyRepaymentMinPercent { get; set; }
        public decimal EarlyRepaymentRateReduction { get; set; }
        public decimal MinAnnualInterestRate { get; set; }
    }

    public class AcceptLoanOfferDto
    {
        public int? TargetCardId { get; set; }          // verilməsə əvvəl seçilən kart qalır
    }

    public class PayLoanDto
    {
        public int CardId { get; set; }
        public string Type { get; set; } = "Installment";   // Installment, PartialEarly, FullEarly
        public decimal Amount { get; set; }                  // yalnız PartialEarly üçün

        // Forma hər açılanda yeni GUID yaranır: eyni sorğu iki dəfə göndərilsə (ikiqat klik, yeniləmə) pul bir dəfə çıxır
        public string? RequestId { get; set; }
    }

    public class LoanReceiptDto
    {
        public string Reference { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Penalty { get; set; }
        public decimal Interest { get; set; }
        public decimal Principal { get; set; }
        public string CardLabel { get; set; } = string.Empty;
        public decimal CardBalanceAfter { get; set; }
        public bool LoanClosed { get; set; }
        public decimal? NewInterestRate { get; set; }
        public DateTime PaidAt { get; set; }
    }

    // Ödəniş kodu ilə axtarış (Bills səhifəsi): sahibin adı gizlədilmiş görünür
    public class LoanLookupDto
    {
        public string ContractNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;       // "Elçin H."
        public decimal AnnualInterestRate { get; set; }
        public decimal DueTotal { get; set; }
        public decimal OverdueTotal { get; set; }
        public DateTime? NextDueDate { get; set; }
        public bool CanPay { get; set; }
    }

    public class PayByCodeDto
    {
        public string ContractNumber { get; set; } = string.Empty;
        public int CardId { get; set; }
        public decimal Amount { get; set; }                 // 0 = həmin ayın bütün borcu; azdırsa hissə-hissə ödəniş (qalanı başqa kartdan)
        public string? RequestId { get; set; }
    }
}
