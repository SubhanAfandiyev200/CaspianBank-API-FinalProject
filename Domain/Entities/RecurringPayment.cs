using Domain.Common;

namespace Domain.Entities
{
    // Müştərinin aylıq təkrarlanan ödənişi: hər ayın 1-i seçilmiş kartdan eyni məbləğ çıxır.
    // Kredit üçün ProviderId boşdur və Identifier kredit kodudur (məbləğ yoxdur: həmin ayın aylıq ödənişi ödənilir)
    public class RecurringPayment : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int? ProviderId { get; set; }                        // null = Caspian krediti
        public BillProvider? Provider { get; set; }

        public string Identifier { get; set; } = string.Empty;      // 077 nömrəsi (0771420618), abunəçi kodu və ya KR-2026-000123
        public decimal Amount { get; set; }                         // kredit üçün 0

        public int CardId { get; set; }
        public Card Card { get; set; } = null!;

        public bool IsActive { get; set; } = true;                  // müştəri dayandıra bilər

        // "2026-10": hansı ay üçün ödəniş artıq işlənib. Yaradılanda cari ay yazılır (ilk ödəniş elə indi edilir), ona görə növbəti ödəniş gələn ayın 1-ində olur
        public string LastPeriod { get; set; } = string.Empty;

        // Uğursuz cəhd olsa eyni gün təkrar yoxlanmasın (gündə bir dəfə cəhd): Bakı günü "2026-11-01"
        public string? LastAttemptDay { get; set; }
        public string? LastError { get; set; }                      // son cəhd uğursuzdursa səbəb, yoxsa boş
    }
}
