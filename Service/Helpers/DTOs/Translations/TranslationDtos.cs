namespace Service.Helpers.DTOs.Translations
{
    public class LanguageDto
    {
        public string Code { get; set; } = string.Empty;        // en, az, ru
        public string Name { get; set; } = string.Empty;        // English, Azərbaycanca, Русский
    }

    public class TranslationFilterDto
    {
        public string? Search { get; set; }
        public bool OnlyMissing { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    // Admin siyahısı: bir açarın (ingilis mətnin) bütün dillərdəki tərcüməsi
    public class TranslationRowDto
    {
        public string Key { get; set; } = string.Empty;
        public string Az { get; set; } = string.Empty;
        public string Ru { get; set; } = string.Empty;
    }

    public class TranslationPageDto
    {
        public List<TranslationRowDto> Items { get; set; } = new List<TranslationRowDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class SaveTranslationDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Az { get; set; }
        public string? Ru { get; set; }
    }
}
