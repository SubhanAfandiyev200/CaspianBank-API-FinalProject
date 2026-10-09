using Microsoft.AspNetCore.Http;

namespace Service.Services.Interfaces
{
    public interface IFileService
    {
        // Şəkli yoxlayıb saxlayır, saytda açılan yolu qaytarır (məs. /images/brands/3f2a....png)
        Task<string> UploadFileAsync(IFormFile file, string folder);

        // MP4 və ya WebM videonu yoxlayıb saxlayır (ən çox 50 MB), yolu qaytarır (məs. /videos/about/3f2a....mp4)
        Task<string> UploadVideoAsync(IFormFile file, string folder);

        // Yuxarıdakı iki metodun qaytardığı yolu verirsən. Yalnız bu sistemin yüklədiyi şəkli və ya videonu silir
        Task DeleteFileAsync(string webPath);
    }
}
