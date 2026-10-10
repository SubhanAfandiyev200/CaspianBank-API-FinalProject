namespace Service.Helpers
{
    // Video faylı qaydaları (About blokunun videosu). Tip faylın adına yox, İÇİNDƏKİ imzaya görə təyin olunur: MP4 və WebM
    public static class VideoFileRules
    {
        public const long MaxBytes = 50L * 1024 * 1024; // 50 MB

        // Faylın ilk baytları kifayətdir (bütün video yaddaşa oxunmur)
        public const int HeaderBytes = 12;

        public static string? DetectExtension(byte[] header)
        {
            // MP4: 5-8-ci baytlar "ftyp"
            if (header.Length >= 8
                && header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p')
            {
                return ".mp4";
            }

            // WebM (Matroska/EBML): 1A 45 DF A3
            if (header.Length >= 4
                && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
            {
                return ".webm";
            }

            return null;
        }
    }
}
