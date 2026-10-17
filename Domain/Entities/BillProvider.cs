using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Ödəniş təchizatçısı (operator və ya kommunal şirkət). Adı, loqosu və aktivliyi admin paneldən dəyişir; qaydalar (prefikslər, tarif) koddadır
    public class BillProvider : BaseEntity
    {
        public string Code { get; set; } = string.Empty;            // azercell, nar, azerishiq... (unikal, dəyişmir)
        public string Name { get; set; } = string.Empty;
        public BillCategory Category { get; set; }
        public string? LogoUrl { get; set; }                        // /images/providers/azercell.png; boşdursa baş hərflərdən işarə göstərilir
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Mobil operator: nömrənin ilk iki rəqəmi (0-dan sonra). Məs. "70,77" (Nar)
        public string? Prefixes { get; set; }

        // Kommunal və internet: abunəçi kodunun rəqəm sayı (təchizatçıdan təchizatçıya fərqlənir)
        public int IdentifierLength { get; set; }
        public string IdentifierLabel { get; set; } = string.Empty; // "Phone number", "Subscriber code"

        // İşlətməyə görə hesab (işıq, qaz, su): vahid və 1 vahidin qiyməti. Paketli xidmətdə boş
        public string? UnitName { get; set; }                       // kWh, m³
        public decimal UnitPrice { get; set; }                      // AZN

        public decimal MinPayment { get; set; }
        public decimal MaxPayment { get; set; }

        public ICollection<BillSubscriber> Subscribers { get; set; } = new List<BillSubscriber>();
    }
}
