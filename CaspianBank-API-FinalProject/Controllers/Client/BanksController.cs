using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Kart nömrəsinin ilk 8 rəqəmindən (BIN) bankı tanıyır: köçürmə formasında nömrə sahəsinin üstündə bankın loqosu və adı çıxır
    [Route("api/banks")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("transfer")]
    public class BanksController : ControllerBase
    {
        private readonly IBankDirectoryService _service;

        public BanksController(IBankDirectoryService service)
        {
            _service = service;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? bin)
        {
            var result = await _service.LookupAsync(bin);
            if (result.IsNotFound)
            {
                return NotFound(new { isSuccess = false, errors = new[] { "Bank not recognized." } });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
