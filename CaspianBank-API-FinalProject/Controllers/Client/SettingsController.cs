using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Saytın ümumi ayarları (loqo, sayt adı və s.): login tələb etmir, bütün səhifələrin layout-u istifadə edir
    [Route("api/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public SettingsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            return Ok(await _settingService.GetAllUIAsync());
        }
    }
}
