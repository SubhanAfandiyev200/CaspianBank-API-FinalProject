using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.AboutPillars;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // About blokunun altındakı sütunlar (şəkil, başlıq, açıqlama). Mətn bloku AboutController-dədir
    [Route("api/admin/about-pillars")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AboutPillarController : ControllerBase
    {
        private readonly IAboutPillarService _pillarService;
        public AboutPillarController(IAboutPillarService pillarService)
        {
            _pillarService = pillarService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPillars()
        {
            return Ok(await _pillarService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPillarDetail([FromRoute] int id)
        {
            return Ok(await _pillarService.GetDetailAsync(id));
        }
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePillar([FromForm] CreateAboutPillarDto request)
        {
            await _pillarService.CreateAsync(request);
            return Ok();
        }
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePillar([FromRoute] int id, [FromForm] UpdateAboutPillarDto request)
        {
            await _pillarService.UpdateAsync(id, request);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePillar([FromRoute] int id)
        {
            await _pillarService.DeleteAsync(id);
            return NoContent();
        }
    }
}
