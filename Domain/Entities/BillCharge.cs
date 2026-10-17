using Domain.Common;

namespace Domain.Entities
{
    // Abunəçiyə bir ay üçün yazılan hesab (internet paketi və ya işlətmə). (SubscriberId, Period) unikaldır: bir ay yalnız bir dəfə yazılır
    public class BillCharge : BaseEntity
    {
        public int SubscriberId { get; set; }
        public BillSubscriber Subscriber { get; set; } = null!;

        public string Period { get; set; } = string.Empty;          // 2026-10
        public decimal Amount { get; set; }
        public decimal? Usage { get; set; }                         // kWh və ya m³ (paketli xidmətdə boş)
        public string Description { get; set; } = string.Empty;
    }
}
