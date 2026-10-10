using Domain.Entities;

namespace Repository.Models
{
    // Tarixçə səhifəsinin bazadan gələn nəticəsi: bir səhifə sətir + filtrə uyğun BÜTÜN əməliyyatların sayı və cəmləri
    public class TransactionPageResult
    {
        public IReadOnlyList<Transaction> Items { get; set; } = new List<Transaction>();
        public int TotalCount { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
    }
}
