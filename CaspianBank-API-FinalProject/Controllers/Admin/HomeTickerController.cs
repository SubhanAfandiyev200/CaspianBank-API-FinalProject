using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.HomeTickers;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/tickers")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class HomeTickerController : ControllerBase
    {
        private readonly IHomeTickerService _homeTickerService;
        public HomeTickerController(IHomeTickerService homeTickerService)
        {
            _homeTickerService = homeTickerService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllTickersAsync()
        {
            return Ok(await _homeTickerService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTickerDetailAsync([FromRoute] int id)
        {
            return Ok(await _homeTickerService.GetDetailAsync(id));
        }
        [HttpPost]
        public async Task<IActionResult> CreateTicker([FromBody] CreateHomeTickerDto request)
        {
            return Ok(await _homeTickerService.CreateAsync(request));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicker(int id, [FromBody] UpdateHomeTickerDto request)
        {
            return Ok(await _homeTickerService.UpdateAsync(id, request));
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicker(int id)
        {
            await _homeTickerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
