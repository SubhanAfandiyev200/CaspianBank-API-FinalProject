namespace Repository.Results
{
    // Bir ay ərzində bankın real gəliri və cashback xərci (kart jurnalı və kredit ödənişləri qeydlərindən)
    public class MonthMoney
    {
        public decimal Commissions { get; set; }        // köçürmə komissiyası
        public decimal CardFees { get; set; }           // yeni kartın açılış haqqı
        public decimal LoanInterest { get; set; }       // kredit ödənişlərinin faiz hissəsi
        public decimal Penalties { get; set; }          // gecikmə cərimələri
        public decimal Cashback { get; set; }           // müştərilərə ödənilən cashback (bankın xərci)
        public decimal DepositInterest { get; set; }    // depozitlər üçün müştərilərə ödənilən faiz (bankın xərci)
    }

    // Hazırkı vəziyyət (aya aid olmayan hissə) və ayın müştəri sayı
    public class BankSnapshot
    {
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }               // seçilmiş ayda qeydiyyatdan keçənlər
        public int RestrictedUsers { get; set; }
        public decimal CustomerDeposits { get; set; }   // müştəri kartlarındakı pulun cəmi
        public decimal TermDeposits { get; set; }       // müddətli depozitlərdə saxlanılan əsas məbləğ (aktiv)
        public decimal TermDepositInterestOwed { get; set; }   // aktiv depozitlər üçün gələcəkdə ödəniləcək faiz
        public decimal LoansOutstanding { get; set; }   // verilib hələ qayıtmayan əsas borc
        public int PendingLoans { get; set; }
        public int PendingTickets { get; set; }
    }
}
