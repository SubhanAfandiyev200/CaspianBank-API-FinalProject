using Service.Helpers.DTOs.Translations;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Saytın dilləri: tərcümələr verilənlər bazasındadır, MVC onları API-dən alır. Açar ingilis mətnin özüdür
    public interface ITranslationService
    {
        IEnumerable<LanguageDto> GetLanguages();

        // az və ru üçün tərcümə lüğəti (ingilis üçün boş: açar özü mətndir). Naməlum dil xətadır
        Task<ServiceResult<Dictionary<string, string>>> GetDictionaryAsync(string language);

        Task<ServiceResult<TranslationPageDto>> GetPageAsync(TranslationFilterDto filter);
        Task<ServiceResult<TranslationRowDto>> SaveAsync(SaveTranslationDto model);
    }
}
