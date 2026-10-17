using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin ödənişləri (mobil, işıq, qaz, su, internet). UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    [Route("api/bills")]
    [ApiController]
    [Authorize]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillsController(IBillService billService)
        {
            _billService = billService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Səhifədəki təchizatçı seçimi (admin söndürdükləri göstərilmir)
        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            return Ok(await _billService.GetProvidersAsync());
        }

        // Bu ay nə qədər ödənilib və son ödənişlər
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            return Ok(await _billService.GetOverviewAsync(UserId));
        }

        // Nömrə və ya abunəçi kodu ilə axtarış. Kod təxmin edilməsin deyə sürət limiti var
        [EnableRateLimiting("transfer")]
        [HttpPost("lookup")]
        public async Task<IActionResult> Lookup([FromBody] LookupBillDto model)
        {
            return ToResponse(await _billService.LookupAsync(model));
        }

        // Ödəniş: pul ödəyənin öz kartından çıxır
        [EnableRateLimiting("transfer")]
        [HttpPost("pay")]
        public async Task<IActionResult> Pay([FromBody] PayBillDto model)
        {
            return ToResponse(await _billService.PayAsync(UserId, model));
        }

        private IActionResult ToResponse<T>(ServiceResult<T> result)
        {
            if (result.IsNotFound)
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            return Ok(result.Data);
        }
    }
}
