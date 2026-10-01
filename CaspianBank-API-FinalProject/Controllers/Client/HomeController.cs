using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeTickerService _tickerService;
        private readonly IBrandService _brandService;

        public HomeController(IHomeTickerService tickerService,
                              IBrandService brandService)
        {
            _tickerService = tickerService;
            _brandService = brandService;
        }

        [HttpGet("tickers")]
        public async Task<IActionResult> GetAllTickersAsync()
        {
            return Ok(await _tickerService.GetAllUIAsync());
        }
        [HttpGet("brands")]
        public async Task<IActionResult> GetAllBrandsAsync()
        {
            return Ok(await _brandService.GetAllUIAsync());
        }
    }
}
