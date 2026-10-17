using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.ServiceSections;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Services blokunun başlığı. Tək yazıdır: yalnız oxunur və dəyişdirilir. Kartlar ServiceItemController-dədir
    [Route("api/admin/service-section")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ServiceSectionController : ControllerBase
    {
        private readonly IServiceSectionService _serviceSectionService;
        public ServiceSectionController(IServiceSectionService serviceSectionService)
        {
            _serviceSectionService = serviceSectionService;
        }
        [HttpGet]
        public async Task<IActionResult> GetServiceSection()
        {
            return Ok(await _serviceSectionService.GetAsync());
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServiceSection([FromRoute] int id, [FromBody] ServiceSectionUpdateDto request)
        {
            await _serviceSectionService.UpdateAsync(id, request);
            return NoContent();
        }
    }
}
