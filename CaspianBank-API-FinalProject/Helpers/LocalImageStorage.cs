using Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Şəkilləri API-nin wwwroot qovluğuna yazır. Fayl adı həmişə təsadüfidir (istifadəçinin göndərdiyi ad istifadə olunmur).
    public partial class LocalImageStorage : IImageStorage
    {
        private readonly string _webRoot;

        public LocalImageStorage(IWebHostEnvironment environment)
        {
            _webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        public async Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken = default)
        {
            var directory = Path.Combine(_webRoot, "images", folder);
            Directory.CreateDirectory(directory);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content, cancellationToken);

            return $"/images/{folder}/{fileName}";
        }

        public void Delete(string webPath)
        {
            // Yalnız /images/<qovluq>/<32 simvollu təsadüfi ad>.<uzantı> formasındakı fayllar silinir:
            // başlanğıc şəkillər (regular.png və s.) və yolu manipulyasiya etmək cəhdləri nəzərə alınmır
            var match = DeletablePath().Match(webPath ?? string.Empty);
            if (!match.Success) return;

            var full = Path.GetFullPath(Path.Combine(_webRoot, "images", match.Groups["folder"].Value, match.Groups["file"].Value));
            var allowedRoot = Path.GetFullPath(Path.Combine(_webRoot, "images")) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) return;

            if (File.Exists(full)) File.Delete(full);
        }

        [GeneratedRegex(@"^/images/(?<folder>[a-z0-9-]+)/(?<file>[0-9a-f]{32}\.(png|jpg|webp))$")]
        private static partial Regex DeletablePath();
    }
}
