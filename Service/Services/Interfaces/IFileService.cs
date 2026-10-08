using Microsoft.AspNetCore.Http;

namespace Service.Services.Interfaces
{
    public interface IFileService
    {
        // Şəkli yoxlayıb saxlayır, saytda açılan yolu qaytarır (məs. /images/brands/3f2a....png)
        Task<string> UploadFileAsync(IFormFile file, string folder);

        // UploadFileAsync-in qaytardığı yolu verirsən. Yalnız bu sistemin yüklədiyi şəkli silir
        Task DeleteFileAsync(string webPath);
    }
}
