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

    // Admin CMS səhifəsində bir dil seçilib: həmin dildəki tərcümələr açarlar (ingilis mətnlər) üzrə soruşulur
    public class LookupTranslationsDto
    {
        public string Language { get; set; } = string.Empty;    // az və ya ru
        public List<string> Keys { get; set; } = new List<string>();
    }

    // Eyni dil üçün bir neçə mətnin tərcüməsini birlikdə yazır (boş dəyər tərcüməni silir)
    public class SaveContentTranslationsDto
    {
        public string Language { get; set; } = string.Empty;    // az və ya ru
        public List<ContentTranslationItemDto> Items { get; set; } = new List<ContentTranslationItemDto>();
    }

    public class ContentTranslationItemDto
    {
        public string Key { get; set; } = string.Empty;         // ingilis mətn (səhifədəki cari mətn)
        public string? Value { get; set; }
    }
}
