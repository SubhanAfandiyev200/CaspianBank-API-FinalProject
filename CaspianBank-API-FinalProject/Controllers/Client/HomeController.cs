using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeTickerService _tickerService;

        public HomeController(IHomeTickerService tickerService)
        {
            _tickerService = tickerService;
        }

        [HttpGet("tickers")]
        public async Task<IActionResult> GetAllUIAsync()
        {
            return Ok(await _tickerService.GetAllAsync());
        }
    }
}
