using CaspianBank_API_FinalProject.Requests;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers;
using Service.Helpers.DTOs.CardDesigns;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home səhifəsindəki kart dizaynlarını idarə edir (WebDesigner, Admin, SuperAdmin)
    [Route("api/admin/card-designs")]
    [ApiController]
    [Authorize(Roles = Roles.DesignStaff)]
    public class CardDesignController : ControllerBase
    {
        private readonly ICardDesignService _service;

        public CardDesignController(ICardDesignService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CardDesignForm form)
        {
            var (image, error) = await ReadImageAsync(form.Image);
            if (error is not null)
                return BadRequest(new { isSuccess = false, errors = new[] { error } });

            var result = await _service.CreateAsync(new CreateCardDesignDto
            {
                Title = form.Title,
                ShowOnHome = form.ShowOnHome,
                DisplayOrder = form.DisplayOrder,
                Image = image
            });

            if (!result.IsSuccess)
                return BadRequest(new { isSuccess = false, errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] CardDesignForm form)
        {
            var (image, error) = await ReadImageAsync(form.Image);
            if (error is not null)
                return BadRequest(new { isSuccess = false, errors = new[] { error } });

            var result = await _service.UpdateAsync(id, new UpdateCardDesignDto
            {
                Title = form.Title,
                ShowOnHome = form.ShowOnHome,
                DisplayOrder = form.DisplayOrder,
                Image = image
            });

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess)
                return BadRequest(new { isSuccess = false, errors = result.Errors });

            return Ok(result.Data);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.IsSuccess ? NoContent() : NotFound();
        }

        // Faylı yaddaşa oxumazdan əvvəl ölçüsünə baxılır (böyük fayl göndərib serveri yükləməyə qarşı)
        private static async Task<(UploadedImage? Image, string? Error)> ReadImageAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return (null, null);

            if (file.Length > ImageFileRules.MaxBytes)
                return (null, "The image must be up to 2 MB.");

            using var stream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(stream);
            return (new UploadedImage { Content = stream.ToArray() }, null);
        }
    }
}
