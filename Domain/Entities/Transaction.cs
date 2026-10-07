using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    // Kartın balansını dəyişən hər əməliyyatın qeydi (jurnal). Bir köçürmə bir neçə sətirdən ibarətdir
    // (göndərənin çıxışı, komissiya, alanın girişi): onlar eyni Reference ilə bağlıdır.
    public class Transaction : BaseEntity
    {
        public int CardId { get; set; }                 // balansı dəyişən kart
        public Card Card { get; set; } = null!;

        public TransactionType Type { get; set; }
        public bool IsIncome { get; set; }              // true: karta pul girdi, false: kartdan çıxdı
        public decimal Amount { get; set; }             // həmişə müsbət
        public decimal BalanceAfter { get; set; }       // bu əməliyyatdan sonra kartın balansı

        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;   // eyni əməliyyatın sətirlərini birləşdirir (məs. TR-3F9A...)
        public string? Note { get; set; }               // köçürmədə istifadəçinin qeydi
    }
}
