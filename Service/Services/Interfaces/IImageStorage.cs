namespace Service.Services.Interfaces
{
    public interface IImageStorage
    {
        // Şəkli saxlayır, saytda açılan yolu qaytarır (məs. /images/cards/3f2a....png)
        Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken = default);

        // Yalnız bu sistemin yüklədiyi şəkli silir (başlanğıc/default şəkillərə toxunmur)
        void Delete(string webPath);
    }
}
