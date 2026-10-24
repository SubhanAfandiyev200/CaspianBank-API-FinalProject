using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Deposits;
using Service.Services.Interfaces;
using System.Globalization;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Depozitlər: bütün işçilər siyahını görür. Faiz və hədləri yalnız SuperAdmin, Admin və Accountant dəyişir
    [Route("api/admin/deposits")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant + "," + Roles.CustomerSupport + "," + Roles.Security)]
    public class DepositsAdminController : ControllerBase
    {
        private const string EditRoles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant;

        private readonly IDepositService _service;
        private readonly IAuditService _audit;

        public DepositsAdminController(IDepositService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] DepositFilterDto filter)
        {
            var result = await _service.GetPageAsync(filter);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }

        [Authorize(Roles = EditRoles)]
        [HttpPut("plans/{id:int}")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdateDepositPlanDto model)
        {
            var result = await _service.UpdatePlanAsync(id, model);
            if (result.IsNotFound)
            {
                return NotFound(new { isSuccess = false, errors = result.Errors });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }

            var plan = result.Data!;
            await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), AuditActions.DepositPlanChanged, "DepositPlan",
                plan.TermMonths + " months",
                plan.AnnualRate.ToString("0.##", CultureInfo.InvariantCulture) + "% a year, " + plan.MinAmount.ToString("0.##", CultureInfo.InvariantCulture)
                + " to " + plan.MaxAmount.ToString("0.##", CultureInfo.InvariantCulture) + " AZN, " + (plan.IsActive ? "active" : "turned off"));
            return Ok(plan);
        }
    }
}
