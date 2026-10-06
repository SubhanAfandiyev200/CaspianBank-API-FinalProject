namespace Service.Helpers
{
    // Şəkil faylı qaydaları. Tip faylın adına/uzantısına yox, İÇİNDƏKİ imzaya (magic bytes) görə təyin olunur:
    // adı .png olan amma əslində başqa fayl olanı qəbul etmirik.
    public static class ImageFileRules
    {
        public const int MaxBytes = 2 * 1024 * 1024; // 2 MB

        public static string? DetectExtension(byte[] content)
        {
            if (content.Length >= 8
                && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47
                && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
                return ".png";

            if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
                return ".jpg";

            if (content.Length >= 12
                && content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F'
                && content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P')
                return ".webp";

            return null;
        }
    }
}
