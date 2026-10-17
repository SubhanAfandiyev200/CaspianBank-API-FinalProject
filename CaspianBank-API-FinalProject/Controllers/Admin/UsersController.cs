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

        public UsersController(IUserAdminService service)
        {
            _service = service;
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
            return ToResponse(await _service.RestrictAsync(StaffName, id, model));
        }

        [Authorize(Roles = RestrictRoles)]
        [HttpPost("{id}/unrestrict")]
        public async Task<IActionResult> Unrestrict(string id)
        {
            return ToResponse(await _service.UnrestrictAsync(id));
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
