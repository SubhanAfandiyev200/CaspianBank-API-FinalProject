using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.Bills
{
    // Ödəniş təchizatçısı (səhifədəki seçim)
    public class BillProviderDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;        // Mobile, Electricity, Gas, Water, Internet
        public string? LogoUrl { get; set; }
        public string IdentifierLabel { get; set; } = string.Empty; // "Phone number", "Subscriber code"
        public string IdentifierHint { get; set; } = string.Empty;  // "055 123 45 67" və ya "10 digits"
        public string? PrefixesText { get; set; }                   // "070 or 077" (mobil)
        public int IdentifierLength { get; set; }
        public string? UnitName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MinPayment { get; set; }
        public decimal MaxPayment { get; set; }
    }

    public class LookupBillDto
    {
        public int ProviderId { get; set; }
        public string Identifier { get; set; } = string.Empty;
    }

    // Nömrə və ya abunəçi kodu ilə axtarışın nəticəsi: kimin hesabıdır və nə qədər borcu var
    public class BillLookupDto
    {
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Identifier { get; set; } = string.Empty;      // göstərilən forma (055 123 45 67)
        public string SubscriberName { get; set; } = string.Empty;  // kommunalda tam ad, mobildə maskalanmış
        public decimal Debt { get; set; }                           // 0 = borc yoxdur
        public decimal Credit { get; set; }                         // qabaqcadan ödənilmiş balans
        public decimal SuggestedAmount { get; set; }                // borc varsa borc, yoxdursa boş
        public decimal? MonthlyFee { get; set; }
        public string? PackageName { get; set; }
        public string? LastChargePeriod { get; set; }
        public decimal? LastChargeAmount { get; set; }
        public decimal? LastChargeUsage { get; set; }
        public string? UnitName { get; set; }
        public decimal MinPayment { get; set; }
        public decimal MaxPayment { get; set; }
    }

    public class PayBillDto
    {
        public int ProviderId { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int CardId { get; set; }

        // Forma hər açılanda yeni GUID yaranır: eyni sorğu iki dəfə göndərilsə (ikiqat klik, yeniləmə) pul bir dəfə çıxır
        public string? RequestId { get; set; }
    }

    public class BillReceiptDto
    {
        public string Reference { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal DebtCleared { get; set; }
        public decimal CreditAdded { get; set; }
        public decimal RemainingDebt { get; set; }                  // ödənişdən sonra hələ qalan borc
        public decimal BalanceAfter { get; set; }                   // ödənişdən sonra abunəçinin balansı (borc yoxdursa)
        public decimal CashbackEarned { get; set; }
        public string CardLabel { get; set; } = string.Empty;
        public decimal CardBalanceAfter { get; set; }
        public DateTime PaidAt { get; set; }
    }

    // "Son ödənişlər" cədvəlinin sətri
    public class BillPaymentDto
    {
        public int Id { get; set; }
        public DateTime PaidAt { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal CashbackEarned { get; set; }
        public string CardLabel { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    // Bills səhifəsinin üst xülasəsi və son ödənişlər
    public class BillsOverviewDto
    {
        public decimal PaidThisMonth { get; set; }
        public int PaymentsThisMonth { get; set; }
        public decimal CashbackThisMonth { get; set; }
        public List<BillPaymentDto> RecentPayments { get; set; } = new List<BillPaymentDto>();
    }

    // ---------- Admin ----------

    public class BillProviderAdminDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    // Adı, aktivliyi və loqonu dəyişir; kod, kateqoriya və qaydalar dəyişmir
    public class UpdateBillProviderDto
    {
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public IFormFile? Logo { get; set; }
    }
}
