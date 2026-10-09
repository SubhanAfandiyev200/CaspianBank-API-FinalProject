using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.BenefitItems;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/benefit-items")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class BenefitItemController : ControllerBase
    {
        private readonly IBenefitItemService _benefitItemService;
        public BenefitItemController(IBenefitItemService benefitItemService)
        {
            _benefitItemService = benefitItemService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBenefitItems()
        {
            return Ok(await _benefitItemService.GetAllAsync());
        }
        // Düymə üçün hazır yerlər (Create/Edit formasındakı siyahı)
        [HttpGet("destinations")]
        public IActionResult GetDestinations()
        {
            return Ok(_benefitItemService.GetDestinations());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBenefitItemDetail([FromRoute] int id)
        {
            return Ok(await _benefitItemService.GetDetailAsync(id));
        }
        [HttpPost]
        public async Task<IActionResult> CreateBenefitItem([FromBody] BenefitItemCreateDto request)
        {
            await _benefitItemService.CreateAsync(request);
            return Ok();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBenefitItem([FromRoute] int id, [FromBody] BenefitItemUpdateDto request)
        {
            await _benefitItemService.UpdateAsync(id, request);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBenefitItem([FromRoute] int id)
        {
            await _benefitItemService.DeleteAsync(id);
            return NoContent();
        }
    }
}
