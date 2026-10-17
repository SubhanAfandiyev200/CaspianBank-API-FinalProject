using Domain.Common;

namespace Domain.Entities
{
    // Caspian Bank-ın özü: BIN, kredit və köçürmə qaydaları bir yerdə, ki admin kodu dəyişmədən tənzimləsin. Cədvəldə tək sətir olur
    public class OwnBank : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Bin { get; set; } = string.Empty;                 // 96301020

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
        public int OfferValidDays { get; set; }                         // 7 (azaldılmış məbləğ təklifinin etibarlılıq müddəti, gün)

        // Vaxtından əvvəl hissəvi ödəniş: əsas borcun ən azı EarlyRepaymentMinPercent faizi qədər ödənəndə illik faiz
        // EarlyRepaymentRateReduction qədər azalır, lakin MinAnnualInterestRate-dən aşağı düşmür
        public decimal MinEarlyPayment { get; set; }                    // 10 (hissəvi ödənişin minimum məbləği, AZN)
        public decimal EarlyRepaymentMinPercent { get; set; }           // 10
        public decimal EarlyRepaymentRateReduction { get; set; }        // 1 (faiz bəndi: 10% -> 9%)
        public decimal MinAnnualInterestRate { get; set; }              // 5

        // Banklararası köçürmə qaydaları. Ayrıca banklararası komissiya YOXDUR (hər köçürməyə yalnız aylıq limit komissiyası tətbiq olunur),
        // InterbankFeePercent və InterbankMinFee istifadə edilmir (0 saxlanılır). Qüvvədə olan: MaxInterbankAmount
        public decimal InterbankFeePercent { get; set; }                // 0 (istifadə edilmir)
        public decimal? InterbankMinFee { get; set; }
        public decimal MaxInterbankAmount { get; set; }                 // 10000

        // Bankın maliyyə qaydaları (Bank dashboard-un hesabı bunlara əsaslanır)
        public decimal OwnFunds { get; set; } = 2400000m;                // bankın öz vəsaiti (ehtiyat), AZN
        public decimal OperatingCostMonthly { get; set; } = 150m;       // aylıq əməliyyat xərci (maaşlar, infrastruktur), AZN
        public decimal ProfitTaxPercent { get; set; } = 20m;             // mənfəət vergisi %
        public decimal ReservePercent { get; set; } = 10m;               // müştəri depozitlərinin neçə faizi məcburi ehtiyatdır
    }
}
