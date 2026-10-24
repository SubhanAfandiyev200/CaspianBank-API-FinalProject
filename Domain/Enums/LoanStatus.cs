namespace Domain.Enums
{
    // Caspian kreditinin vəziyyəti. Axın:
    //   Pending -> Rejected | Active (tam məbləğ təsdiqlənsə) | Offered (azaldılmış məbləğ təklif olunsa)
    //   Offered -> Active (müştəri razılaşır) | Declined (imtina edir və ya təklifin vaxtı bitir)
    //   Active  -> Closed | Court (90 gün gecikmə)
    //   Court   -> Closed
    //   Qara siyahıdakı müştəri: Pending -> AwaitingSuperAdmin (kredit şöbəsi təsdiq edir) -> Super Admin təsdiq edir: Active və ya Offered, rədd edir: Rejected
    public enum LoanStatus
    {
        Pending,    // müraciət gözləyir, işçi baxmayıb
        Rejected,   // rədd edilib
        Offered,    // azaldılmış məbləğ təklif olunub, müştərinin cavabı gözlənilir
        Declined,   // müştəri təklifdən imtina edib və ya təklifin vaxtı bitib
        Active,     // pul verilib, ödənişlər gedir
        Closed,     // tam ödənilib (vaxtında və ya vaxtından əvvəl)
        Court,      // uzun gecikmə, məhkəməyə keçib
        AwaitingSuperAdmin  // qara siyahıdakı müştəri: kredit şöbəsi təsdiq edib, Super Admin-in son təsdiqi gözlənilir
    }
}
