namespace Service.Helpers.DTOs.Bank
{
    public class DashboardMonthOptionDto
    {
        public string Value { get; set; } = string.Empty;   // "2026-10"
        public string Label { get; set; } = string.Empty;   // "October 2026"
    }

    // Bank dashboard: seçilmiş ayın real rəqəmləri. Home səhifəsindəki şişirdilmiş rəqəmlər burada YOXDUR
    public class BankDashboardDto
    {
        public string Period { get; set; } = string.Empty;
        public string PeriodLabel { get; set; } = string.Empty;
        public List<DashboardMonthOptionDto> Months { get; set; } = new List<DashboardMonthOptionDto>();

        // Müştərilər və pul (hazırkı vəziyyət)
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }
        public decimal CustomerDeposits { get; set; }
        public decimal LoansOutstanding { get; set; }

        // Gəlir
        public decimal TransferCommissions { get; set; }
        public decimal CardOpeningFees { get; set; }
        public decimal LoanInterest { get; set; }
        public decimal LatePenalties { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal? IncomeChangePercent { get; set; }       // əvvəlki aya nisbətən, əvvəlki ay 0-dırsa boş

        // Xərc
        public decimal CashbackPaid { get; set; }
        public decimal OperatingCosts { get; set; }
        public decimal ProfitTaxPercent { get; set; }
        public decimal ProfitTax { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal? ExpenseChangePercent { get; set; }
        public decimal ProfitBeforeTax { get; set; }
        public decimal NetProfit { get; set; }

        // Vergi və ehtiyat
        public decimal QuarterTaxAccrued { get; set; }          // rübün seçilmiş aya qədər yığılmış vergisi
        public decimal ReservePercent { get; set; }
        public decimal RequiredReserve { get; set; }
        public decimal OwnFunds { get; set; }
        public decimal ReserveAfterTax { get; set; }
        public decimal? CoverageRatio { get; set; }             // öz vəsait / (məcburi ehtiyat + rüblük vergi)
        public bool ReserveHealthy { get; set; }

        // Gözləyən işlər
        public int PendingLoans { get; set; }
        public int PendingTickets { get; set; }
        public int RestrictedAccounts { get; set; }
    }
}
