using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.BenefitSections;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Benefits blokunun başlığı. Tək yazıdır: yalnız oxunur və dəyişdirilir. Kartlar BenefitItemController-dədir
    [Route("api/admin/benefit-section")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class BenefitSectionController : ControllerBase
    {
        private readonly IBenefitSectionService _benefitSectionService;
        public BenefitSectionController(IBenefitSectionService benefitSectionService)
        {
            _benefitSectionService = benefitSectionService;
        }
        [HttpGet]
        public async Task<IActionResult> GetBenefitSection()
        {
            return Ok(await _benefitSectionService.GetAsync());
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBenefitSection([FromRoute] int id, [FromBody] BenefitSectionUpdateDto request)
        {
            await _benefitSectionService.UpdateAsync(id, request);
            return NoContent();
        }
    }
}
