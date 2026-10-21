using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Reports;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Müştərilərin şübhəli əməliyyat bildirişləri: bütün işçilər siyahını görür, qərarı yalnız SuperAdmin, Admin və Security verir
    [Route("api/admin/reports")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant + "," + Roles.CustomerSupport + "," + Roles.Security)]
    public class ReportsAdminController : ControllerBase
    {
        private const string ResolveRoles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Security;

        private readonly IReportAdminService _service;
        private readonly IAuditService _audit;

        public ReportsAdminController(IReportAdminService service, IAuditService audit)
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
        public async Task<IActionResult> GetPage([FromQuery] ReportFilterDto filter)
        {
            return ToResponse(await _service.GetPageAsync(filter));
        }

        [Authorize(Roles = ResolveRoles)]
        [HttpPost("{id:int}/resolve")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveReportDto model)
        {
            var result = await _service.ResolveAsync(StaffName, id, model);
            if (result.IsSuccess)
            {
                var report = result.Data!;
                var action = report.Status == "ActionTaken" ? AuditActions.ReportActionTaken
                    : report.Status == "Dismissed" ? AuditActions.ReportDismissed
                    : AuditActions.ReportReviewed;
                await _audit.LogAsync(StaffId, action, "Report", report.TransactionReference,
                    report.ReporterName + (string.IsNullOrEmpty(report.ActionTaken) ? string.Empty : ": " + report.ActionTaken)
                    + (string.IsNullOrEmpty(report.ResolutionNote) ? string.Empty : " (" + report.ResolutionNote + ")"));
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
