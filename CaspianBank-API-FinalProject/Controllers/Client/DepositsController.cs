using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Deposits;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin müddətli depozitləri. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox.
    // Müddət bitməmiş pulu çıxarmaq üçün endpoint yoxdur: pul müddət bitəndə özü karta qayıdır
    [Route("api/deposits")]
    [ApiController]
    [Authorize]
    public class DepositsController : ControllerBase
    {
        private readonly IDepositService _depositService;

        public DepositsController(IDepositService depositService)
        {
            _depositService = depositService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Variantlar (müddət, faiz, hədlər), müştərinin depozitləri və cəmlər
        [HttpGet]
        public async Task<IActionResult> GetOverview()
        {
            return Ok(await _depositService.GetOverviewAsync(UserId));
        }

        // Depozit aç: pul seçilmiş kartdan çıxır
        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Open([FromBody] OpenDepositDto model)
        {
            var result = await _depositService.OpenAsync(UserId, model);
            if (result.IsNotFound)
            {
                return NotFound(new { isSuccess = false, errors = result.Errors });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
