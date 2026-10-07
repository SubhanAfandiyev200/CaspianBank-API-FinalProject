namespace Repository.Results
{
    // Ödənişli kartın açılması (haqq çıxılır + kart yaranır) nəticəsi
    public enum CardOpenResult
    {
        Success,
        FundingCardNotFound,
        FundingCardBlocked,
        InsufficientFunds,
        Conflict        // eyni anda başqa əməliyyat balansı dəyişdi: yenidən cəhd etmək lazımdır
    }
}
