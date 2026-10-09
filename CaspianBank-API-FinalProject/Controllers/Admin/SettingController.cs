using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Settings;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Saytın ümumi ayarları (loqo, şirkət adı, ünvan, email, telefon, footer mətnləri). Açarlar sabitdir: yalnız oxunur və dəyərləri dəyişdirilir.
    // Loqo şəkildir, ona görə ayrıca endpoint-lə yüklənir
    [Route("api/admin/settings")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class SettingController : ControllerBase
    {
        private readonly ISettingService _settingService;
        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllSettings()
        {
            return Ok(await _settingService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSettingDetail([FromRoute] int id)
        {
            return Ok(await _settingService.GetByIdAsync(id));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSetting([FromRoute] int id, [FromBody] UpdateSettingDto request)
        {
            await _settingService.UpdateAsync(id, request);
            return NoContent();
        }
        [HttpPut("{id}/logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateLogo([FromRoute] int id, [FromForm] UpdateSettingLogoDto request)
        {
            await _settingService.UpdateLogoAsync(id, request);
            return NoContent();
        }
    }
}
