using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Support;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Hər səhifədəki "Help" pəncərəsi: giriş etməyən ziyarətçi də yaza bilər (email məcburidir). Daxil olmuş müştərinin mesajı onun
    // yazışmasına düşür. İşçilər bildiriş alır. Spam olmasın deyə giriş endpoint-ləri kimi sürət limiti var
    [Route("api/help")]
    [ApiController]
    [AllowAnonymous]
    public class HelpController : ControllerBase
    {
        private readonly ISupportTicketService _service;

        public HelpController(ISupportTicketService service)
        {
            _service = service;
        }

        [EnableRateLimiting("auth")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Send([FromForm] SendHelpDto model)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var result = await _service.SendHelpAsync(userId, model);
            if (result.IsNotFound)
            {
                return Unauthorized();
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
