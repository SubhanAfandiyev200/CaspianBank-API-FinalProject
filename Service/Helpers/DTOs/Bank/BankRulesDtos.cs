namespace Service.Helpers.DTOs.Bank
{
    // Bankın dəyişdirilə bilən qaydaları (Accountant, Admin, SuperAdmin). Kredit qaydaları yalnız YENİ kreditlərə aiddir,
    // artıq verilmiş kreditlərin faizi və cəriməsi onların imzalandığı andakı kimi qalır
    public class BankRulesDto
    {
        // Maliyyə
        public decimal ReservePercent { get; set; }                 // müştəri pulunun neçə faizi məcburi ehtiyatdır
        public decimal ProfitTaxPercent { get; set; }
        public decimal OperatingCostMonthly { get; set; }
        public decimal MaxInterbankAmount { get; set; }             // başqa banka bir dəfəyə ən çox

        // Kredit
        public decimal MinLoanAmount { get; set; }
        public decimal MaxLoanAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public decimal FixedPenalty { get; set; }
        public decimal DailyPenaltyPercent { get; set; }
        public int MaxActiveLoans { get; set; }
        public int OfferValidDays { get; set; }
    }
}
