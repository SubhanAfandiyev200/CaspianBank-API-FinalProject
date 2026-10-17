namespace Domain.Enums
{
    public enum TransactionType
    {
        TopUp,              // xarici kartdan balans artırma
        TransferOut,        // köçürmə (göndərən kartın çıxışı)
        TransferIn,         // köçürmə (alan kartın girişi)
        Commission,         // köçürmə komissiyası
        CardFee,            // yeni kartın açılış haqqı
        LoanDisbursement,   // kredit məbləği karta düşdü
        LoanPayment,        // kredit ödənişi (kartdan çıxış)
        BillPayment,        // kommunal, mobil və internet ödənişi (kartdan çıxış)
        Cashback            // ödənişdən yığılan cashback (Cashback kartına giriş)
    }
}
