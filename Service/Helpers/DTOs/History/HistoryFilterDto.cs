namespace Service.Helpers.DTOs.History
{
    // GET api/history sorğusunun parametrləri (hamısı istəyə bağlıdır)
    public class HistoryFilterDto
    {
        public int? CardId { get; set; }

        // TransactionType adı: TopUp, TransferOut, TransferIn, Commission, CardFee
        public string? Type { get; set; }

        // "in" (karta pul girdi) və ya "out" (kartdan çıxdı)
        public string? Direction { get; set; }

        // Tarixlər (gün) Bakı vaxtı ilə: From günün əvvəlindən, To günün sonuna qədər daxildir
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Reference, Description və ya Note içində axtarış
        public string? Search { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
