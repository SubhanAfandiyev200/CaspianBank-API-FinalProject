namespace Domain.Constants
{
    // Settings cədvəlindəki açarlar. Sayt bunları adla oxuyur, ona görə admin yeni açar əlavə etmir və açarın adını dəyişmir: yalnız dəyərləri dəyişir
    public static class SettingKeys
    {
        public const string Logo = "Logo";                          // şəkil yoludur, mətnlə yox, şəkil yükləməklə dəyişir
        public const string CompanyName = "CompanyName";
        public const string Address = "Address";
        public const string Email = "Email";
        public const string PhoneNumber = "PhoneNumber";
        public const string FooterTitle = "FooterTitle";
        public const string FooterDescription = "FooterDescription";
        public const string Copyright = "Copyright";
        public const string InstagramUrl = "InstagramUrl";          // footer-dakı sosial şəbəkə linkləri: tam ünvan (https://...)
        public const string FacebookUrl = "FacebookUrl";
        public const string TwitterUrl = "TwitterUrl";
        public const string LinkedInUrl = "LinkedInUrl";
        public const string MapLatitude = "MapLatitude";            // Contact us səhifəsindəki xəritənin mərkəzi: onluq kəsr (məsələn 40.3730), nöqtə ilə
        public const string MapLongitude = "MapLongitude";
    }
}
