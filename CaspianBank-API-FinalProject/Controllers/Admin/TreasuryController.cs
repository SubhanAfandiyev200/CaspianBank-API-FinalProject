using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Bank;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Bankın xəzinəsi: balans, məcburi ehtiyat və jurnal (hər giriş və çıxış). Yalnız oxunur; maliyyə məlumatıdır, ona görə SuperAdmin, Admin və Accountant
    [Route("api/admin/treasury")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant)]
    public class TreasuryController : ControllerBase
    {
        private readonly ITreasuryService _service;

        public TreasuryController(ITreasuryService service)
        {
            _service = service;
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
