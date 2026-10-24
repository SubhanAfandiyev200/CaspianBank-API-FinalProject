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
        Cashback,           // ödənişdən yığılan cashback (Cashback kartına giriş)
        DepositOpen,        // müddətli depozit açıldı (kartdan çıxış)
        DepositPayout,      // depozitin əsas məbləği müddət bitəndə karta qayıtdı
        DepositInterest,    // depozitin faizi müddət bitəndə karta düşdü (bankın xərci)
        TopUpOut            // başqa Caspian kartının balansını artırmaq üçün kartdan çıxan pul
    }
}
