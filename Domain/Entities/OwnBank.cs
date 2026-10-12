using Domain.Common;

namespace Domain.Entities
{
    // Caspian Bank-ın özü: BIN, kredit və köçürmə qaydaları bir yerdə, ki admin kodu dəyişmədən tənzimləsin. Cədvəldə tək sətir olur
    public class OwnBank : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Bin { get; set; } = string.Empty;                 // 11223456

        // Kredit qaydaları
        public decimal MinLoanAmount { get; set; }                      // 500
        public decimal MaxLoanAmount { get; set; }                      // 50000
        public int MinTermMonths { get; set; }                          // 6
        public int MaxTermMonths { get; set; }                          // 48
        public decimal AnnualInterestRate { get; set; }                 // 10 (faiz %)
        public int MinAge { get; set; }                                 // 18
        public decimal FixedPenalty { get; set; }                       // 10 (gecikmədə bir dəfəlik cərimə, AZN)
        public decimal DailyPenaltyPercent { get; set; }                // 0.5 (gecikmiş məbləğə gündə %)
        public int CourtAfterDays { get; set; }                         // 90 (bu qədər gecikmədən sonra məhkəməyə)
        public int MaxActiveLoans { get; set; }                         // 3
        public decimal? MaxPaymentToIncomePercent { get; set; }         // 45 (aylıq ödəniş maaşın ən çox bu faizi)

        // Banklararası köçürmə qaydaları
        public decimal InterbankFeePercent { get; set; }                // 1
        public decimal? InterbankMinFee { get; set; }
        public decimal MaxInterbankAmount { get; set; }                 // 10000
    }
}
