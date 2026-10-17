using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Loans;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Kredit baxışı: müraciətlərin siyahısı, müraciətçinin tam məlumatı (reyestr, iş yeri, başqa banklardakı kreditlər), təsdiq və rədd.
    // Pul qərarlarını Accountant, Admin və SuperAdmin verir
    [Route("api/admin/loans")]
    [ApiController]
    [Authorize(Roles = Roles.Accountant + "," + Roles.Admin + "," + Roles.SuperAdmin)]
    public class LoansController : ControllerBase
    {
        private readonly ILoanReviewService _reviewService;
        private readonly IAuditService _audit;

        public LoansController(ILoanReviewService reviewService, IAuditService audit)
        {
            _reviewService = reviewService;
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
        public async Task<IActionResult> GetPage([FromQuery] LoanReviewFilterDto filter)
        {
            return ToResponse(await _reviewService.GetPageAsync(filter));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            return ToResponse(await _reviewService.GetDetailAsync(id));
        }

        // Təsdiq: məbləğ istənilənə bərabərdirsə kredit dərhal verilir, azdırsa müştəriyə təklif gedir
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveLoanDto model)
        {
            var result = await _reviewService.ApproveAsync(StaffUserId, id, model);
            if (result.IsSuccess)
            {
                var offered = result.Data!.Status == "Offered";
                await _audit.LogAsync(StaffUserId, offered ? AuditActions.LoanOffered : AuditActions.LoanApproved, "Loan", result.Data.ContractNumber,
                    (offered ? "Offered " : "Approved ") + model.ApprovedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)
                    + " AZN of " + result.Data.RequestedAmount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN requested");
            }
            return ToResponse(result);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectLoanDto model)
        {
            var result = await _reviewService.RejectAsync(StaffUserId, id, model);
            if (result.IsSuccess)
            {
                await _audit.LogAsync(StaffUserId, AuditActions.LoanRejected, "Loan", result.Data!.ContractNumber, model.Reason);
            }
            return ToResponse(result);
        }

        private IActionResult ToResponse<T>(ServiceResult<T> result)
        {
            if (result.IsNotFound)
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            return Ok(result.Data);
        }
    }
}
