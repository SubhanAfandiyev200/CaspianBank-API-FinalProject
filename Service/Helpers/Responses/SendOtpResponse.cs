namespace Service.Helpers.Responses
{
    public class SendOtpResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();

        // Yeni kod istəmək üçün gözləmə lazımdırsa saniyə sayı (controller 429 qaytarır)
        public int RetryAfterSeconds { get; set; }

        // Yalnız SMTP qurulmayıb və Development rejimidir: kod ekranda göstərilsin deyə qaytarılır
        public string? DevCode { get; set; }
    }
}
