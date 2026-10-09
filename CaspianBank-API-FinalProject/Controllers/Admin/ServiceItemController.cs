using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.ServiceItems;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home-dakı altı xidmət kartı. Hər kart proqramın bir bölməsinə aparır, ona görə yalnız mətnləri dəyişir (əlavə/silmə yoxdur)
    [Route("api/admin/service-items")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class ServiceItemController : ControllerBase
    {
        private readonly IServiceItemService _serviceItemService;
        public ServiceItemController(IServiceItemService serviceItemService)
        {
            _serviceItemService = serviceItemService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllServiceItems()
        {
            return Ok(await _serviceItemService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceItemDetail([FromRoute] int id)
        {
            return Ok(await _serviceItemService.GetDetailAsync(id));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServiceItem([FromRoute] int id, [FromBody] ServiceItemUpdateDto request)
        {
            await _serviceItemService.UpdateAsync(id, request);
            return NoContent();
        }
    }
}
