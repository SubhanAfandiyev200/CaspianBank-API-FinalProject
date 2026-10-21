using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Saytın dilləri və tərcümələri: giriş etməmiş ziyarətçi də görür (login səhifəsi də tərcümə olunur)
    [Route("api/i18n")]
    [ApiController]
    [AllowAnonymous]
    public class I18nController : ControllerBase
    {
        private readonly ITranslationService _service;

        public I18nController(ITranslationService service)
        {
            _service = service;
        }

        [HttpGet("languages")]
        public IActionResult Languages()
        {
            return Ok(_service.GetLanguages());
        }

        // en üçün boş lüğət, az və ru üçün açar (ingilis mətn) → tərcümə
        [HttpGet("{language}")]
        public async Task<IActionResult> Dictionary(string language)
        {
            var result = await _service.GetDictionaryAsync(language);
            if (result.IsNotFound)
            {
                return NotFound(new { isSuccess = false, errors = new[] { "Unknown language." } });
            }
            return Ok(result.Data);
        }
    }
}
