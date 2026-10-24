using Domain.Common;

namespace Domain.Entities
{
    // Müştərinin aylıq təkrarlanan köçürməsi: hər ayın seçilmiş günü (1-28) kartdan eyni məbləğ köçürülür.
    // Alıcı ya müştərinin öz kartıdır (ToCardId), ya da kart nömrəsidir (başqa müştəri və ya başqa bank). Köçürmə adi köçürmə kimi icra olunur
    // (komissiya, limit, Cashback qaydaları və hesabın dondurulması eynidir), ona görə pul çatmasa və ya kart bloklanıbsa uğursuz olur
    public class RecurringTransfer : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int SourceCardId { get; set; }
        public Card SourceCard { get; set; } = null!;

        public int? ToCardId { get; set; }                          // öz kartına
        public string? ToCardNumber { get; set; }                   // başqa müştərinin və ya başqa bankın kartı (16 rəqəm)
        public string ToLabel { get; set; } = string.Empty;         // siyahıda göstərilən alıcı: "Elnur M. · Kapital Bank •••• 4825"

        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public int DayOfMonth { get; set; }                         // 1-28 (hər ayda var)

        public bool IsActive { get; set; } = true;

        // "2026-10": hansı ay üçün köçürmə artıq edilib. Yaradılanda elə qoyulur ki, ilk köçürmə növbəti gələn seçilmiş gündə olsun
        public string LastPeriod { get; set; } = string.Empty;

        // Uğursuz cəhd olsa eyni gün təkrar yoxlanmasın (gündə bir dəfə): Bakı günü "2026-11-05"
        public string? LastAttemptDay { get; set; }
        public string? LastError { get; set; }                      // son cəhd uğursuzdursa səbəb, yoxsa boş
    }
}
