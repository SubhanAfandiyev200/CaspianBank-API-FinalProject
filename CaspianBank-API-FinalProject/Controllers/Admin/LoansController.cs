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

        public LoansController(ILoanReviewService reviewService)
        {
            _reviewService = reviewService;
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
            return ToResponse(await _reviewService.ApproveAsync(StaffUserId, id, model));
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectLoanDto model)
        {
            return ToResponse(await _reviewService.RejectAsync(StaffUserId, id, model));
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
