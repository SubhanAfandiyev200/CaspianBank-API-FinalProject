using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Bank dashboard: gəlir, xərc, vergi, ehtiyat və müştərilər. Bütün işçilər görür; rəqəmlər real əməliyyatlardan hesablanır
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant + "," + Roles.CustomerSupport + "," + Roles.Security)]
    public class DashboardController : ControllerBase
    {
        private readonly IBankDashboardService _service;

        public DashboardController(IBankDashboardService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? month)
        {
            var result = await _service.GetAsync(month);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
