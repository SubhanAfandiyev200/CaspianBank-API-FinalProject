using CaspianBank_API_FinalProject.Requests;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.CardDesigns;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home səhifəsindəki kart dizaynlarını idarə edir (WebDesigner, Admin, SuperAdmin)
    [Route("api/admin/card-designs")]
    [ApiController]
    [Authorize(Roles = Roles.WebDesigner + "," + Roles.Admin + "," + Roles.SuperAdmin)]
    public class CardDesignController : ControllerBase
    {
        private readonly ICardDesignService _service;

        public CardDesignController(ICardDesignService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CardDesignForm form)
        {
            // Şəklin ölçüsünü və növünü FileService yoxlayır, yanlışdırsa global exception middleware 400 qaytarır
            var result = await _service.CreateAsync(new CreateCardDesignDto
            {
                Title = form.Title,
                ShowOnHome = form.ShowOnHome,
                DisplayOrder = form.DisplayOrder,
                Image = form.Image
            });

            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }

            return Ok(result.Data);
        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] CardDesignForm form)
        {
            var result = await _service.UpdateAsync(id, new UpdateCardDesignDto
            {
                Title = form.Title,
                ShowOnHome = form.ShowOnHome,
                DisplayOrder = form.DisplayOrder,
                Image = form.Image
            });

            if (result.IsNotFound)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }

            return Ok(result.Data);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.IsSuccess ? NoContent() : NotFound();
        }
    }
}
