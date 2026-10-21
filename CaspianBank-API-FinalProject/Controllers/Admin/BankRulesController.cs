using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Bank;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Bankın qaydaları (rezerv, vergi, əməliyyat xərci, banklararası limit, kredit qaydaları). Maliyyə qərarıdır: SuperAdmin, Admin və Accountant
    [Route("api/admin/bank-rules")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin + "," + Roles.Accountant)]
    public class BankRulesController : ControllerBase
    {
        private readonly IBankRulesService _service;
        private readonly IAuditService _audit;

        public BankRulesController(IBankRulesService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _service.GetAsync();
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BankRulesDto model)
        {
            var result = await _service.UpdateAsync(model);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }

            var (rules, changes) = result.Data;
            if (changes != "No change")
            {
                await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), AuditActions.BankRulesChanged, "Bank", "Caspian Bank", changes);
            }
            return Ok(rules);
        }
    }
}
