namespace Domain.Enums
{
    // Bildirişin növü: səhifədə hansı ikonun göstəriləcəyini seçir
    public enum NotificationCategory
    {
        Loan,       // kredit: təklif, təsdiq, gecikmə, məhkəmə
        Transfer,   // köçürmə: pul gəldi
        Card,       // kart: bloklandı, blokdan çıxarıldı
        Staff       // işçilər üçün: yeni müraciət və s.
    }
}
