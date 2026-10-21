using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Translations;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class TranslationService : ITranslationService
    {
        private const int MaxPageSize = 100;
        private static readonly string[] Translated = { "az", "ru" };

        private readonly ITranslationRepository _translations;

        public TranslationService(ITranslationRepository translations)
        {
            _translations = translations;
        }

        public IEnumerable<LanguageDto> GetLanguages()
        {
            return new[]
            {
                new LanguageDto { Code = "en", Name = "English" },
                new LanguageDto { Code = "az", Name = "Azərbaycanca" },
                new LanguageDto { Code = "ru", Name = "Русский" }
            };
        }

        public async Task<ServiceResult<Dictionary<string, string>>> GetDictionaryAsync(string language)
        {
            var code = (language ?? string.Empty).Trim().ToLowerInvariant();
            if (code == "en")
            {
                return ServiceResult<Dictionary<string, string>>.Ok(new Dictionary<string, string>());
            }
            if (!Translated.Contains(code))
            {
                return ServiceResult<Dictionary<string, string>>.NotFound();
            }
            return ServiceResult<Dictionary<string, string>>.Ok(await _translations.GetDictionaryAsync(code));
        }

        public async Task<ServiceResult<TranslationPageDto>> GetPageAsync(TranslationFilterDto filter)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 30 : Math.Min(filter.PageSize, MaxPageSize);
            var (keys, total) = await _translations.GetKeysPageAsync(filter.Search, filter.OnlyMissing, Translated, page, pageSize);
            var rows = await _translations.GetForKeysAsync(keys);

            var items = keys.Select(k => new TranslationRowDto
            {
                Key = k,
                Az = rows.FirstOrDefault(r => r.Key == k && r.Language == "az")?.Value ?? string.Empty,
                Ru = rows.FirstOrDefault(r => r.Key == k && r.Language == "ru")?.Value ?? string.Empty
            }).ToList();

            return ServiceResult<TranslationPageDto>.Ok(new TranslationPageDto
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))
            });
        }

        public async Task<ServiceResult<TranslationRowDto>> SaveAsync(SaveTranslationDto model)
        {
            var key = (model.Key ?? string.Empty).Trim();
            if (key.Length == 0 || key.Length > 400)
            {
                return ServiceResult<TranslationRowDto>.Fail("The text to translate must be 1 to 400 characters.");
            }
            var az = model.Az?.Trim() ?? string.Empty;
            var ru = model.Ru?.Trim() ?? string.Empty;
            if (az.Length > 2000 || ru.Length > 2000)
            {
                return ServiceResult<TranslationRowDto>.Fail("A translation can be at most 2000 characters.");
            }

            await _translations.UpsertAsync("az", key, az);
            await _translations.UpsertAsync("ru", key, ru);
            return ServiceResult<TranslationRowDto>.Ok(new TranslationRowDto { Key = key, Az = az, Ru = ru });
        }
    }
}
