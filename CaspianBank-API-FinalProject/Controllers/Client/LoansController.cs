using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin kreditləri. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox:
    // kimsə başqasının kreditinə baxa, onun təklifinə cavab verə və ya onun adına erkən ödəniş edə bilməz.
    [Route("api/loans")]
    [ApiController]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Müraciət formasının qaydaları (məbləğ aralığı, müddətlər, faiz) və müştərinin müraciət edə bilib bilməməsi
        [HttpGet("options")]
        public async Task<IActionResult> GetOptions()
        {
            return ToResponse(await _loanService.GetOptionsAsync(UserId));
        }

        // Mənim kreditlərim və müraciətlərim
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _loanService.GetMyLoansAsync(UserId));
        }

        // Bir kreditin səhifəsi: cədvəl, ödənişlər, indi ödənilməli məbləğ
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOne(int id)
        {
            return ToResponse(await _loanService.GetMyLoanAsync(UserId, id));
        }

        // Yeni müraciət
        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] ApplyLoanDto model)
        {
            return ToResponse(await _loanService.ApplyAsync(UserId, model));
        }

        // Azaldılmış məbləğ təklifinə razılıq: kredit aktivləşir, pul karta düşür
        [EnableRateLimiting("transfer")]
        [HttpPost("{id:int}/accept")]
        public async Task<IActionResult> Accept(int id, [FromBody] AcceptLoanOfferDto? model)
        {
            return ToResponse(await _loanService.AcceptOfferAsync(UserId, id, model ?? new AcceptLoanOfferDto()));
        }

        [EnableRateLimiting("transfer")]
        [HttpPost("{id:int}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            return ToResponse(await _loanService.DeclineOfferAsync(UserId, id));
        }

        // Ödəniş: aylıq, hissəvi erkən və ya tam bağlama (Type sahəsi). Pul ödəyənin öz kartından çıxır
        [EnableRateLimiting("transfer")]
        [HttpPost("{id:int}/pay")]
        public async Task<IActionResult> Pay(int id, [FromBody] PayLoanDto model)
        {
            return ToResponse(await _loanService.PayAsync(UserId, id, model));
        }

        // Ödəniş kodu ilə axtarış (Bills): sahibin adı gizlədilmiş görünür. Kod təxmin edilməsin deyə sürət limiti var
        [EnableRateLimiting("transfer")]
        [HttpGet("lookup/{contractNumber}")]
        public async Task<IActionResult> Lookup(string contractNumber)
        {
            return ToResponse(await _loanService.LookupAsync(contractNumber));
        }

        // Ödəniş kodu ilə aylıq ödəniş (Bills): kodu bilən hər kəs ödəyə bilər
        [EnableRateLimiting("transfer")]
        [HttpPost("pay-by-code")]
        public async Task<IActionResult> PayByCode([FromBody] PayByCodeDto model)
        {
            return ToResponse(await _loanService.PayByCodeAsync(UserId, model));
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
