using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Users;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Müştərilərin siyahısı və profili: bütün işçilər görür. Hesabı dondurmaq və açmaq yalnız Admin, SuperAdmin və Security-dir
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant + "," + Roles.CustomerSupport + "," + Roles.Security)]
    public class UsersController : ControllerBase
    {
        private const string RestrictRoles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Security;

        private readonly IUserAdminService _service;
        private readonly IAuditService _audit;

        public UsersController(IUserAdminService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        private string StaffUserId
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
        public async Task<IActionResult> GetPage([FromQuery] UserFilterDto filter)
        {
            return ToResponse(await _service.GetPageAsync(filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(string id)
        {
            return ToResponse(await _service.GetDetailAsync(id));
        }

        [Authorize(Roles = RestrictRoles)]
        [HttpPost("{id}/restrict")]
        public async Task<IActionResult> Restrict(string id, [FromBody] RestrictUserDto model)
        {
            var result = await _service.RestrictAsync(StaffName, id, model);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(StaffUserId, AuditActions.AccountRestricted, "Customer", result.Data!.Name + " " + result.Data.Surname, model.Reason);
            }
            return ToResponse(result);
        }

        [Authorize(Roles = RestrictRoles)]
        [HttpPost("{id}/unrestrict")]
        public async Task<IActionResult> Unrestrict(string id)
        {
            var result = await _service.UnrestrictAsync(id);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(StaffUserId, AuditActions.AccountRestored, "Customer", result.Data!.Name + " " + result.Data.Surname, "Account restored");
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
