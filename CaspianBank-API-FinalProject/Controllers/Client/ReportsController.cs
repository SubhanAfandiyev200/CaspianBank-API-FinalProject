using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Reports;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin şübhəli əməliyyat bildirişləri (History-dəki Report düyməsi). UserId HƏMİŞƏ tokendən götürülür
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
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

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _service.GetMineAsync(UserId));
        }

        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Report([FromBody] ReportTransactionDto model)
        {
            var result = await _service.ReportAsync(UserId, model);
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
