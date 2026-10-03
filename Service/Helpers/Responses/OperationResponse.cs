namespace Service.Helpers.Responses
{
    // Sadə "uğurlu/uğursuz + xətalar" cavabı (forgot-password, reset-password və s.)
    public class OperationResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();

        // Yalnız SMTP qurulmayıb və Development rejimidir: reset linki ekranda göstərilsin deyə qaytarılır
        public string? DevLink { get; set; }
    }
}
