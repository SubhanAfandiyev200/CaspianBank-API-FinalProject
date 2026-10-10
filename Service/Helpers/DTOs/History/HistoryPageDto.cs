using Service.Helpers.DTOs.Cards;

namespace Service.Helpers.DTOs.History
{
    // Tarixçənin bir səhifəsi + filtrə uyğun bütün əməliyyatların sayı və cəmləri
    public class HistoryPageDto
    {
        public IEnumerable<TransactionDto> Items { get; set; } = new List<TransactionDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
    }
}
