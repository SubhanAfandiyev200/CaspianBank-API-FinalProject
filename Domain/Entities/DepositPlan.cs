using Domain.Common;

namespace Domain.Entities
{
    // Müddətli depozit variantı: müddət, illik faiz və məbləğ hədləri. Admin faizi və hədləri dəyişə bilər, müddət sabitdir
    public class DepositPlan : BaseEntity
    {
        public int TermMonths { get; set; }             // 3, 6, 12, 24
        public decimal AnnualRate { get; set; }         // illik faiz, % (sadə faiz)
        public decimal MinAmount { get; set; }          // 100
        public decimal MaxAmount { get; set; }          // 100000
        public bool IsActive { get; set; } = true;      // söndürülən variantda yeni depozit açılmır, açılmışlar davam edir
    }
}
