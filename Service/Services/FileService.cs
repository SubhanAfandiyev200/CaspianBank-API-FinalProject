using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Service.Helpers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Service.Services
{
    // Şəkil və video yükləmə və silmə (kart dizaynı, brend, About videosu və s.). Şəkillər wwwroot/images/<qovluq>/, videolar wwwroot/videos/<qovluq>/ altında saxlanılır.
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

        public async Task<string> UploadVideoAsync(IFormFile file, string folder)
        {
            if (file is null || file.Length == 0)
            {
                throw new BadRequestException("Choose a video.");
            }

            if (file.Length > VideoFileRules.MaxBytes)
            {
                throw new BadRequestException("The video must be up to 50 MB.");
            }

            // Video böyükdür: yaddaşa tam oxunmur, yalnız imza üçün ilk baytlar yoxlanılır, qalanı birbaşa fayla yazılır
            using var input = file.OpenReadStream();
            var header = new byte[VideoFileRules.HeaderBytes];
            var read = 0;
            while (read < header.Length)
            {
                var count = await input.ReadAsync(header, read, header.Length - read);
                if (count == 0)
                {
                    break;
                }
                read += count;
            }

            var extension = VideoFileRules.DetectExtension(header.Take(read).ToArray());
            if (extension is null)
            {
                throw new BadRequestException("Only MP4 or WebM videos are allowed.");
            }

            var directory = Path.Combine(_webRoot, "videos", folder);
            Directory.CreateDirectory(directory);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var fullPath = Path.Combine(directory, fileName);

            try
            {
                using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                await output.WriteAsync(header, 0, read);
                await input.CopyToAsync(output);
            }
            catch
            {
                // Yazma yarımçıq qalıbsa qismən fayl qalmasın
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                throw;
            }

            return $"/videos/{folder}/{fileName}";
        }

        public Task DeleteFileAsync(string webPath)
        {
            // Yalnız /images|videos/<qovluq>/<32 simvollu təsadüfi ad>.<uzantı> formasındakı fayllar silinir:
            // başlanğıc fayllar (regular.png, homeVideo.mp4 və s.) və yolu manipulyasiya etmək cəhdləri nəzərə alınmır
            var match = DeletablePath().Match(webPath ?? string.Empty);
            if (!match.Success)
            {
                return Task.CompletedTask;
            }

            var root = match.Groups["root"].Value;
            var full = Path.GetFullPath(Path.Combine(_webRoot, root, match.Groups["folder"].Value, match.Groups["file"].Value));
            var allowedRoot = Path.GetFullPath(Path.Combine(_webRoot, root)) + Path.DirectorySeparatorChar;
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

        [GeneratedRegex(@"^/(?<root>images|videos)/(?<folder>[a-z0-9-]+)/(?<file>[0-9a-f]{32}\.(png|jpg|webp|mp4|webm))$")]
        private static partial Regex DeletablePath();
    }
}
