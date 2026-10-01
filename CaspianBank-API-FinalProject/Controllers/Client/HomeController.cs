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
        private readonly IAboutService _aboutService;
        private readonly IAboutPillarService _pillarService;

        public HomeController(IHomeTickerService tickerService,
                              IBrandService brandService,
                              IAboutService aboutService,
                              IAboutPillarService pillarService)
        {
            _tickerService = tickerService;
            _brandService = brandService;
            _pillarService = pillarService;
            _aboutService = aboutService;

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
        [HttpGet("about")]
        public async Task<IActionResult> GetAboutAsync()
        {
            var about = await _aboutService.GetUIAsync();
            if (about is null) return NotFound();
            return Ok(about);
        }
        [HttpGet("pillars")]
        public async Task<IActionResult> GetAllAboutPillarsAsync()
        {
            return Ok(await _pillarService.GetAllUIAsync());
        }
    }
}
