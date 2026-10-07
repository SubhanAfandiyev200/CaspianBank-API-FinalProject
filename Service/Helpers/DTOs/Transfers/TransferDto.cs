namespace Service.Helpers.DTOs.Transfers
{
    // Köçürmə sorğusu: alıcı ya öz kartdır (ToCardId), ya da başqa müştərinin kart nömrəsidir (ToCardNumber)
    public class TransferDto
    {
        public int FromCardId { get; set; }
        public int? ToCardId { get; set; }
        public string? ToCardNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }

        // Forma hər açılanda yeni GUID yaranır: eyni sorğu iki dəfə göndərilsə (ikiqat klik, səhifəni yeniləmə) pul bir dəfə köçür
        public string? RequestId { get; set; }
    }
}
