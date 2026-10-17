using Domain.Common;

namespace Domain.Entities
{
    // Başqa bankın kartına köçürmənin qeydi. Kart jurnalındakı TransferOut sətri göndərənin balansını göstərir, bu qeyd isə
    // qəbzi yenidən qurmaq üçündür (kimə, hansı banka). Bank adı və ad həmin anın surətidir
    public class ExternalTransfer : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int SourceCardId { get; set; }
        public Card SourceCard { get; set; } = null!;

        public int ExternalCardId { get; set; }
        public ExternalCard ExternalCard { get; set; } = null!;

        public string Reference { get; set; } = string.Empty;       // TR-...: kart jurnalındakı Reference ilə eynidir
        public string BankName { get; set; } = string.Empty;
        public string CardLast4 { get; set; } = string.Empty;
        public string HolderMasked { get; set; } = string.Empty;    // "Elnur M."
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public string? Note { get; set; }
        public decimal SourceBalanceAfter { get; set; }
    }
}
