using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Service.Helpers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Service.Services
{
    // Şəkil yükləmə və silmə (kart dizaynı, brend və s.). Hamısı wwwroot/images/<qovluq>/ altında saxlanılır.
    // Fayl adı həmişə təsadüfidir (müştərinin göndərdiyi ad istifadə olunmur), növ isə adına yox, İÇİNDƏKİ imzaya görə təyin olunur
    public partial class FileService : IFileService
    {
        private readonly string _webRoot;

        public FileService(IWebHostEnvironment environment)
        {
            _webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException("Choose an image.");
            }

            // Faylı yaddaşa oxumazdan əvvəl ölçüsünə baxılır (böyük fayl göndərib serveri yükləməyə qarşı)
            if (file.Length > ImageFileRules.MaxBytes)
            {
                throw new BadRequestException("The image must be up to 2 MB.");
            }

            using var stream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            var extension = ImageFileRules.DetectExtension(content);
            if (extension is null)
            {
                throw new BadRequestException("Only PNG, JPEG or WebP images are allowed.");
            }

            var directory = Path.Combine(_webRoot, "images", folder);
            Directory.CreateDirectory(directory);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content);

            return $"/images/{folder}/{fileName}";
        }

        public Task DeleteFileAsync(string webPath)
        {
            // Yalnız /images/<qovluq>/<32 simvollu təsadüfi ad>.<uzantı> formasındakı fayllar silinir:
            // başlanğıc şəkillər (regular.png və s.) və yolu manipulyasiya etmək cəhdləri nəzərə alınmır
            var match = DeletablePath().Match(webPath ?? string.Empty);
            if (!match.Success)
            {
                return Task.CompletedTask;
            }

            var full = Path.GetFullPath(Path.Combine(_webRoot, "images", match.Groups["folder"].Value, match.Groups["file"].Value));
            var allowedRoot = Path.GetFullPath(Path.Combine(_webRoot, "images")) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(full))
            {
                File.Delete(full);
            }

            return Task.CompletedTask;
        }

        [GeneratedRegex(@"^/images/(?<folder>[a-z0-9-]+)/(?<file>[0-9a-f]{32}\.(png|jpg|webp))$")]
        private static partial Regex DeletablePath();
    }
}
