using Domain.Common;

namespace Domain.Entities
{
    // Müştərinin etdiyi ödəniş. Təchizatçı və abunəçi məlumatı həmin andakı surətdir (loqo, ad sonra dəyişsə də tarixçə eyni qalır)
    public class BillPayment : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int ProviderId { get; set; }
        public BillProvider Provider { get; set; } = null!;
        public int SubscriberId { get; set; }
        public BillSubscriber Subscriber { get; set; } = null!;

        public int CardId { get; set; }
        public Card Card { get; set; } = null!;

        public decimal Amount { get; set; }
        public decimal DebtCleared { get; set; }                    // borcu bağlayan hissə
        public decimal CreditAdded { get; set; }                    // borcdan artıq hissə (balansda qalır)
        public decimal CashbackEarned { get; set; }

        public string Reference { get; set; } = string.Empty;       // BP-3F9A...: kart jurnalındakı Reference ilə eynidir
        public string ProviderName { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
    }
}
