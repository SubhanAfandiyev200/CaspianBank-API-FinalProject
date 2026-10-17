namespace Service.Helpers.Responses
{
    // "Bu sizsiniz?" təsdiqi üçün: vətəndaşın yalnız az məlumatı qaytarılır (soyad qısaldılır)
    public class CheckIdentityResponse
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string Name { get; set; } = string.Empty;
        public string MaskedSurname { get; set; } = string.Empty;      // məs. "M."
        public int BirthYear { get; set; }
        public string? PhotoUrl { get; set; }                          // məs. /citizens/5KM2R9A.jpg
    }
}
