using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Abouts;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home-dakı "About" blokunun mətni. Tək yazıdır: yalnız oxunur və dəyişdirilir. Üç sütun (pillar) AboutPillarController-dədir
    [Route("api/admin/about")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class AboutController : ControllerBase
    {
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
    }
}
