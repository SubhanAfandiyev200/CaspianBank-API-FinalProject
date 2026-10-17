namespace Service.Helpers.Settings
{
    // Home səhifəsindəki "Trust indicators" rəqəmlərinə əlavə olunan başlanğıc dəyərlər (appsettings.json -> HomeStats).
    // Rəqəm = bazadakı real say + bu başlanğıc. Müştəri qeydiyyatdan keçəndə və ya köçürmə edəndə real say artır, göstərilən rəqəm də onunla artır.
    // Bank dashboard (admin) bu dəyərləri İSTİFADƏ ETMİR: orada yalnız real rəqəmlər var. Hamısını 0 etsən Home da real rəqəmləri göstərər
    public class HomeStatsSettings
    {
        public int BaseCustomers { get; set; } = 900;
        public int BaseTransfers { get; set; } = 4200;
        public decimal BaseTransferVolume { get; set; } = 1850000m;
        public int BaseCards { get; set; } = 1750;
    }
}
