using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.BenefitSections;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/benefit-sections")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class BenefitSectionController : ControllerBase
    {
        private readonly IBenefitSectionService _benefitSectionService;
        public BenefitSectionController(IBenefitSectionService benefitSectionService)
        {
            _benefitSectionService = benefitSectionService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBenefitSections()
        {
            return Ok(await _benefitSectionService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBenefitSectionDetail([FromRoute] int id)
        {
            return Ok(await _benefitSectionService.GetDetailAsync(id));
        }
        [HttpPost]
        public async Task<IActionResult> CreateBenefitSection([FromBody] BenefitSectionCreateDto request)
        {
            await _benefitSectionService.CreateAsync(request);
            return Ok();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBenefitSection([FromRoute] int id, [FromBody] BenefitSectionUpdateDto request)
        {
            await _benefitSectionService.UpdateAsync(id, request);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBenefitSection([FromRoute] int id)
        {
            await _benefitSectionService.DeleteAsync(id);
            return NoContent();
        }
    }
}
