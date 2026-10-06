namespace Service.Helpers.DTOs.CardDesigns
{
    // Yüklənmiş şəkil (ASP.NET-dən asılı olmayan sadə forma: bayt massivi)
    public class UploadedImage
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
