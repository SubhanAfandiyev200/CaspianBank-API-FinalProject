using Domain.Common;

namespace Domain.Entities
{
    // Təchizatçının saxta abunəçisi (Caspian müştərisi deyil): mobil nömrə və ya kommunal/internet abunəçi kodu.
    // Balance işarəlidir: mənfi = borc, müsbət = qabaqcadan ödənilmiş məbləğ. Aylıq hesab Balance-ı azaldır (pul çatmasa borca düşür), ödəniş artırır
    public class BillSubscriber : BaseEntity
    {
        public int ProviderId { get; set; }
        public BillProvider Provider { get; set; } = null!;

        public string Identifier { get; set; } = string.Empty;      // mobil: 0551234567, qalanı: abunəçi kodu (yalnız rəqəmlər)
        public string FullName { get; set; } = string.Empty;
        public decimal Balance { get; set; }

        public decimal? MonthlyFee { get; set; }                    // internet paketi: 20, 30 və ya 50 AZN
        public string? PackageName { get; set; }                    // "Home 30"
        public decimal? AverageUsage { get; set; }                  // işıq, qaz, su: aylıq orta işlətmə (kWh və ya m³)

        public bool IsActive { get; set; } = true;
        public byte[] RowVersion { get; set; } = null!;             // ödəniş və aylıq hesab eyni anda balansı dəyişsə biri təkrar oxuyur

        public ICollection<BillCharge> Charges { get; set; } = new List<BillCharge>();
    }
}
