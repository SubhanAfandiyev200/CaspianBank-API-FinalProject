using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Abouts;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home-dakı "About" blokunun mətni və videosu. Tək yazıdır: yalnız oxunur, mətni və videosu dəyişdirilir. Üç sütun (pillar) AboutPillarController-dədir
    [Route("api/admin/about")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class AboutController : ControllerBase
    {
        // Video 50 MB-a qədərdir (FileService yoxlayır). Kestrel-in ümumi 30 MB limiti və form limiti bu əməliyyat üçün yüksəldilir
        private const long VideoRequestBytes = 60L * 1024 * 1024;

        private readonly IAboutService _aboutService;
        public AboutController(IAboutService aboutService)
        {
            _aboutService = aboutService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAbout()
        {
            return Ok(await _aboutService.GetAsync());
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAbout([FromRoute] int id, [FromBody] AboutUpdateDto request)
        {
            await _aboutService.UpdateAsync(id, request);
            return NoContent();
        }
        [HttpPut("{id}/video")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(VideoRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = VideoRequestBytes)]
        public async Task<IActionResult> UpdateAboutVideo([FromRoute] int id, [FromForm] UpdateAboutVideoDto request)
        {
            await _aboutService.UpdateVideoAsync(id, request);
            return NoContent();
        }
    }
}
