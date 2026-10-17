namespace Service.Helpers.DTOs.Bills
{
    // ProviderId boşdursa (və ya 0) ödəniş Caspian kreditinə aiddir: Identifier kredit kodu, Amount lazım deyil
    public class CreateRecurringPaymentDto
    {
        public int? ProviderId { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int CardId { get; set; }
    }

    // Bills səhifəsindəki "Recurring payments" cədvəlinin sətri
    public class RecurringPaymentDto
    {
        public int Id { get; set; }
        public int? ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;        // Mobile, Electricity, Gas, Water, Internet, Loan
        public string? LogoUrl { get; set; }
        public string Identifier { get; set; } = string.Empty;      // göstərilən forma
        public decimal Amount { get; set; }                         // kredit üçün 0 (aylıq ödəniş hər ay özü hesablanır)
        public string CardLabel { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;          // Active, Paused, Failed
        public string? LastError { get; set; }
        public DateTime NextRunDate { get; set; }                   // Bakı günü: gələn ayın 1-i, uğursuzsa və ya gözləyirsə bu gün
    }
}
