using Domain.Entities;

namespace Domain.Loans
{
    // Annuitet cədvəlinin bir sətri (hələ bazaya yazılmayıb)
    public class ScheduleLine
    {
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
    }

    // Kredit kalkulyatorunun nəticəsi
    public class LoanQuote
    {
        public decimal MonthlyPayment { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal TotalInterest { get; set; }
    }

    // Bir aylıq ödənişin indi ödənilməli hissəsi (cərimə, faiz və əsas borc ayrı-ayrı)
    public class InstallmentDue
    {
        public LoanInstallment Installment { get; set; } = null!;
        public decimal Penalty { get; set; }
        public decimal Interest { get; set; }
        public decimal Principal { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }

        public decimal Total
        {
            get
            {
                return Penalty + Interest + Principal;
            }
        }
    }

    // "İndi ödə" düyməsinin məbləği: gecikmiş aylıq ödənişlər və növbəti ödəniş
    public class LoanDue
    {
        public List<InstallmentDue> Items { get; set; } = new List<InstallmentDue>();
        public decimal PenaltyTotal { get; set; }
        public decimal InterestTotal { get; set; }
        public decimal PrincipalTotal { get; set; }
        public decimal Total { get; set; }
        public decimal OverdueTotal { get; set; }
        public int OverdueCount { get; set; }

        public bool HasOverdue
        {
            get
            {
                return OverdueCount > 0;
            }
        }
    }
}
