using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Bank;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Bankın xəzinəsi: balans, məcburi ehtiyat və jurnal (hər giriş və çıxış). Yalnız oxunur; maliyyə məlumatıdır, ona görə SuperAdmin, Admin və Accountant
    [Route("api/admin/treasury")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant)]
    public class TreasuryController : ControllerBase
    {
        private readonly ITreasuryService _service;
        private readonly IAuditService _audit;

        public TreasuryController(ITreasuryService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        // Tamamlanmış ayı bağlayır (əməliyyat xərci və vergi xəzinədən çıxır). Adətən arxa plan işi özü bağlayır; bu, əl ilə bağlamaq üçündür
        [HttpPost("close-month")]
        public async Task<IActionResult> CloseMonth([FromQuery] string? month)
        {
            var result = await _service.CloseMonthAsync(month ?? string.Empty);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            if (!result.Data!.AlreadyClosed)
            {
                await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), AuditActions.TreasuryMonthClosed, "Treasury", result.Data.Month,
                    "Operating costs " + result.Data.OperatingCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN, tax " + result.Data.ProfitTax.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN");
            }
            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] TreasuryFilterDto filter)
        {
            var result = await _service.GetPageAsync(filter);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
