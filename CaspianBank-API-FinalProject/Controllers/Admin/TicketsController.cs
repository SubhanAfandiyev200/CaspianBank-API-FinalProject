using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Support;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Müştərilərin mesajları: CustomerSupport, Admin və SuperAdmin görür, cavab verir və bağlayır. Hər cavab və bağlama audit-ə yazılır
    [Route("api/admin/tickets")]
    [ApiController]
    [Authorize(Roles = Roles.CustomerSupport + "," + Roles.Admin + "," + Roles.SuperAdmin)]
    public class TicketsController : ControllerBase
    {
        private readonly ISupportTicketService _service;
        private readonly IAuditService _audit;

        public TicketsController(ISupportTicketService service, IAuditService audit)
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

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] TicketFilterDto filter)
        {
            return ToResponse(await _service.GetPageAsync(filter));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            return ToResponse(await _service.GetDetailAsync(id));
        }

        [HttpPost("{id:int}/reply")]
        public async Task<IActionResult> Reply(int id, [FromBody] ReplyTicketDto model)
        {
            var result = await _service.StaffReplyAsync(StaffUserId, id, model);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(StaffUserId, AuditActions.TicketAnswered, "Message", result.Data!.Subject, "Replied to " + result.Data.CustomerName);
            }
            return ToResponse(result);
        }

        [HttpPost("{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            var result = await _service.StaffCloseAsync(StaffUserId, id);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(StaffUserId, AuditActions.TicketClosed, "Message", result.Data!.Subject, "Closed the conversation with " + result.Data.CustomerName);
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
