using Domain.Entities;

namespace Repository.Results
{
    public class UserRow
    {
        public AppUser User { get; set; } = null!;
        public int CardCount { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class UserPageResult
    {
        public List<UserRow> Items { get; set; } = new List<UserRow>();
        public int TotalCount { get; set; }
        public int RestrictedCount { get; set; }
    }

    // Müştərinin tam görünüşü üçün lazım olan məlumat (servis DTO-ya çevirir)
    public class UserDetailData
    {
        public AppUser User { get; set; } = null!;
        public List<Card> Cards { get; set; } = new List<Card>();
        public List<Loan> Loans { get; set; } = new List<Loan>();
        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
        public decimal TotalSent { get; set; }
        public int BillPaymentCount { get; set; }
        public decimal BillPaymentTotal { get; set; }
    }
}
