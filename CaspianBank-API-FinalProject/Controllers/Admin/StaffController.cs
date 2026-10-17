using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Staff;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // İşçilər və rolları: yalnız SuperAdmin (Admin rol verə və ala bilməz). Hər dəyişiklik audit log-a yazılır
    [Route("api/admin/staff")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin)]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _service;
        private readonly IAuditService _audit;

        public StaffController(IStaffService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        private string ActorId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        private string ActorName
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            return Ok(await _service.GetListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddStaffDto model)
        {
            var result = await _service.AddAsync(model);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(ActorId, AuditActions.StaffAdded, "Staff", Label(result.Data!), "Role: " + result.Data!.Role);
            }
            return ToResponse(result);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeStaffRoleDto model)
        {
            var result = await _service.ChangeRoleAsync(ActorId, id, model);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(ActorId, AuditActions.StaffRoleChanged, "Staff", Label(result.Data!), "New role: " + result.Data!.Role);
            }
            return ToResponse(result);
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result = await _service.SetActiveAsync(ActorId, ActorName, id, false);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(ActorId, AuditActions.StaffDeactivated, "Staff", Label(result.Data!), "Account switched off");
            }
            return ToResponse(result);
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(string id)
        {
            var result = await _service.SetActiveAsync(ActorId, ActorName, id, true);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(ActorId, AuditActions.StaffActivated, "Staff", Label(result.Data!), "Account switched on");
            }
            return ToResponse(result);
        }

        private static string Label(StaffDto staff)
        {
            return staff.Name + " " + staff.Surname + " (" + staff.Email + ")";
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
