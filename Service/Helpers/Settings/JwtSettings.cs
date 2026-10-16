namespace Service.Helpers.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;

        // Bildiriş hub-ı üçün ayrıca audience və çox qısa müddətli bilet (bağlantı qurulanda yoxlanılır, sonra bağlantı açıq qalır)
        public string HubAudience => Audience + ".Hub";
        public static readonly TimeSpan HubTokenLifetime = TimeSpan.FromMinutes(2);
    }
}
