namespace Service.Helpers.DTOs.Transfers
{
    // "Yoxla və təsdiq et" addımı: pul hələ köçürülməyib
    public class TransferPreviewDto
    {
        public string FromLabel { get; set; } = string.Empty;   // Standard •••• 7245
        public decimal FromBalance { get; set; }
        public string ToLabel { get; set; } = string.Empty;     // öz kart: Silver •••• 1234; başqası: •••• 5678
        public string? ToHolder { get; set; }                   // başqası üçün gizlədilmiş ad: "Nurlan E."
        public bool IsOwn { get; set; }
        public string? ToBankName { get; set; }                 // başqa bankın kartıdırsa bankın adı (Kapital Bank), Caspian-a göndərəndə boş
        public bool IsExternal { get; set; }                    // başqa banka köçürmə
        public decimal MonthlyLimit { get; set; }               // göndərən kartın aylıq pulsuz limiti
        public decimal UsedThisMonth { get; set; }              // bu ay həmin kartdan artıq göndərilən
        public decimal ChargedAmount { get; set; }              // bu köçürmənin limitdən yuxarı olan və komissiya tutulan hissəsi
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public decimal Total { get; set; }
        public string? Note { get; set; }
    }
}
