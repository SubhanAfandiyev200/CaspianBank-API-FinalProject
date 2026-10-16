namespace Domain.Enums
{
    // Kredit ödənişinin növü
    public enum LoanPaymentType
    {
        Installment,    // aylıq ödəniş (gecikmiş olanlar və növbəti ödəniş birlikdə)
        PartialEarly,   // vaxtından əvvəl hissəvi ödəniş: əsas borc azalır, faiz dərəcəsi düşə bilər
        FullEarly       // krediti vaxtından əvvəl tam bağlamaq
    }
}
