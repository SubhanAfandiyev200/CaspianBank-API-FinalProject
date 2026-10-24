using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin xərc analitikası. UserId HƏMİŞƏ tokendən götürülür
    [Route("api/analytics")]
    [ApiController]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _service;

        public AnalyticsController(IAnalyticsService service)
        {
            _service = service;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // months: 1-12 (defolt 6)
        [HttpGet("spending")]
        public async Task<IActionResult> Spending([FromQuery] int months = 6)
        {
            return Ok(await _service.GetSpendingAsync(UserId, months));
        }
    }
}
