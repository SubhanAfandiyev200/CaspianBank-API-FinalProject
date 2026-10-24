using Domain.Common;

namespace Domain.Entities
{
    // Bankın xəzinəsinin jurnalı: bankın öz vəsaitinə hər giriş (+) və çıxış (-). Xəzinənin balansı bu sətirlərin cəmidir.
    // Sətirləri əl ilə yazmaq olmur: AppDbContext.SaveChanges əməliyyat jurnalındakı müvafiq sətirlərlə eyni anda özü yazır
    public class TreasuryEntry : BaseEntity
    {
        public TreasuryEntryKind Kind { get; set; }
        public decimal Amount { get; set; }                 // giriş müsbət, çıxış mənfi
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;   // əməliyyatın kodu (TR-..., LD-..., DP-..., LP-...)
    }

    public enum TreasuryEntryKind
    {
        Capital,            // bankın başlanğıc kapitalı
        Commission,         // köçürmə komissiyası (giriş)
        CardFee,            // kart açılış haqqı (giriş)
        LoanPaidOut,        // verilən kredit (çıxış)
        LoanPrincipal,      // krediti geri qaytarılan əsas borc (giriş)
        LoanInterest,       // kredit faizi (giriş)
        LoanPenalty,        // gecikmə cərimələri (giriş)
        DepositReceived,    // müştərinin depozitə qoyduğu pul (giriş)
        DepositReturned,    // müddət bitəndə müştəriyə qaytarılan əsas məbləğ (çıxış)
        DepositInterest,    // müştəriyə ödənilən depozit faizi (çıxış)
        Cashback,           // müştəriyə ödənilən cashback (çıxış)
        OperatingCost,      // ayın əməliyyat xərci (maaşlar, infrastruktur): ay bağlananda çıxış
        ProfitTax           // ayın mənfəət vergisi: ay bağlananda çıxış
    }
}
