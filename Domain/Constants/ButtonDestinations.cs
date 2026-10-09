namespace Domain.Constants
{
    // Home-dakı düymələrin gedə biləcəyi yerlər (admin linki əl ilə yazmır, siyahıdan seçir). Yol həmişə mövcud səhifədir.
    // "/Account/Welcome" və "/#contact" giriş etmiş istifadəçi üçün MVC-də avtomatik kart əlavə etmə və dəstək səhifəsinə çevrilir (NavLinks.FromDb).
    // Yeni səhifə (kredit, ödənişlər, dəstək) hazır olanda yalnız bu siyahıya bir sətir əlavə olunur.
    public static class ButtonDestinations
    {
        public static readonly (string Path, string Label)[] All =
        {
            ("/Account/Welcome", "Open an account (a new card when logged in)"),
            ("/App", "My cards"),
            ("/App/Transfer", "Transfer money"),
            ("/#contact", "Contact the desk (support when logged in)")
        };

        public static bool Contains(string? path)
        {
            var value = path?.Trim() ?? string.Empty;
            return All.Any(destination => string.Equals(destination.Path, value, StringComparison.Ordinal));
        }
    }
}
