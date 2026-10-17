using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Müştərinin müddətli depoziti. Faiz və müddət açılış anında variantdan köçürülür (sonradan variant dəyişsə, açılmış depozit dəyişmir).
    // Müddət bitməmiş pulu çıxarmaq olmur: bunun üçün heç bir əməliyyat yoxdur. Müddət bitəndə əsas məbləğ və faiz karta qaytarılır
    public class Deposit : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int? PlanId { get; set; }
        public DepositPlan? Plan { get; set; }

        public int TermMonths { get; set; }
        public decimal AnnualRate { get; set; }
        public decimal Principal { get; set; }              // qoyulan məbləğ
        public decimal ExpectedInterest { get; set; }       // müddət sonunda veriləcək faiz (açılışda hesablanır)

        public DateTime StartedAt { get; set; }
        public DateTime MaturesAt { get; set; }

        public int SourceCardId { get; set; }               // pul bu kartdan çıxıb
        public Card SourceCard { get; set; } = null!;
        public int PayoutCardId { get; set; }               // müddət bitəndə pul bu karta qayıdır (kart bloklanıbsa başqa kart seçilə bilər)
        public Card PayoutCard { get; set; } = null!;

        public DepositStatus Status { get; set; } = DepositStatus.Active;
        public DateTime? PaidAt { get; set; }
        public int? PaidToCardId { get; set; }              // pulun həqiqətən qayıtdığı kart

        public string Reference { get; set; } = string.Empty;       // DP-... (açılış), eyni sorğu iki dəfə depozit açmasın
    }
}
