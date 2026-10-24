using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Security;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Şübhəli fəaliyyət siqnalları: Security, Admin və SuperAdmin görür və baxır. Hər qərar audit log-a yazılır
    [Route("api/admin/security/alerts")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Security)]
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityAlertService _service;
        private readonly IAuditService _audit;

        public SecurityController(ISecurityAlertService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        private string StaffId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        private string StaffName
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] SecurityAlertFilterDto filter)
        {
            return ToResponse(await _service.GetPageAsync(filter));
        }

        [HttpPost("{id:int}/resolve")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveAlertDto model)
        {
            var result = await _service.ResolveAsync(StaffName, id, model);
            if (result.IsSuccess)
            {
                var alert = result.Data!;
                var frozen = alert.Status == "Frozen";
                if (frozen)
                {
                    await _audit.LogAsync(StaffId, AuditActions.AccountRestricted, "Customer", alert.CustomerName, "Security alert: " + alert.ResolutionNote);
                }
                await _audit.LogAsync(StaffId, frozen ? AuditActions.SecurityAlertFrozen : AuditActions.SecurityAlertDismissed, "Customer", alert.CustomerName,
                    alert.Kind + " (" + alert.Reference + ")" + (string.IsNullOrEmpty(alert.ResolutionNote) ? string.Empty : ": " + alert.ResolutionNote));
            }
            return ToResponse(result);
        }

        private IActionResult ToResponse<T>(ServiceResult<T> result)
        {
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
