using Domain.Constants;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Bills;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Ödəniş təchizatçılarının adı, loqosu və aktivliyi (Admin, SuperAdmin). Əlavə etmə və silmə yoxdur: təchizatçılar və qaydalar koddadır
    [Route("api/admin/bill-providers")]
    [ApiController]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
    public class BillProviderController : ControllerBase
    {
        private readonly IBillProviderService _service;
        private readonly IAuditService _audit;

        public BillProviderController(IBillProviderService service, IAuditService audit)
        {
            _service = service;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            return Ok(await _service.GetDetailAsync(id));
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateBillProviderDto request)
        {
            await _service.UpdateAsync(id, request);
            await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), AuditActions.BillProviderChanged, "BillProvider", request.Name!.Trim(),
                (request.IsActive ? "Shown to customers" : "Hidden from customers") + (request.Logo is not null && request.Logo.Length > 0 ? ", new logo uploaded" : ""));
            return NoContent();
        }
    }
}
