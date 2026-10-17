using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Audit;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Audit log: kim nə etdi. Həssas olduğu üçün yalnız SuperAdmin, Admin və Security görür. Qeydlər yalnız oxunur (API-dən yazmaq və silmək yoxdur)
    [Route("api/admin/audit")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Security)]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _service;

        public AuditController(IAuditService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] AuditFilterDto filter)
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
