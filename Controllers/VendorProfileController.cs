using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/vendor-profile")]
    [Authorize(Roles = "Vendor")]
    public class VendorProfileController : ControllerBase
    {
        private readonly IVendorProfileService _service;

        public VendorProfileController(IVendorProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _service.GetProfileAsync(GetUserId());
            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] VendorProfileUpdateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _service.UpdateProfileAsync(GetUserId(), request);
            return Ok(updated);
        }

        [HttpPost("logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            try
            {
                var logoUrl = await _service.UploadLogoAsync(GetUserId(), file);
                return Ok(new { logoUrl, message = "Logo updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("logo")]
        public async Task<IActionResult> RemoveLogo()
        {
            await _service.RemoveLogoAsync(GetUserId());
            return NoContent();
        }

        [HttpPost("cover")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCover(IFormFile file)
        {
            try
            {
                var coverUrl = await _service.UploadCoverImageAsync(GetUserId(), file);
                return Ok(new { coverUrl, message = "Cover image updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("cover")]
        public async Task<IActionResult> RemoveCover()
        {
            await _service.RemoveCoverImageAsync(GetUserId());
            return NoContent();
        }

        [HttpPost("gallery")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddGalleryImage(
            IFormFile file,
            [FromForm] string? caption,
            [FromForm] string? category)
        {
            try
            {
                var image = await _service.AddGalleryImageAsync(GetUserId(), file, caption, category);
                return Ok(image);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("gallery/{imageId:int}")]
        public async Task<IActionResult> UpdateGalleryImage(int imageId, [FromBody] VendorGalleryUpdateRequestDto request)
        {
            try
            {
                var updated = await _service.UpdateGalleryImageAsync(GetUserId(), imageId, request);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("gallery/{imageId:int}")]
        public async Task<IActionResult> DeleteGalleryImage(int imageId)
        {
            try
            {
                await _service.DeleteGalleryImageAsync(GetUserId(), imageId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("documents")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            [FromForm] string documentName,
            [FromForm] string documentType)
        {
            try
            {
                var doc = await _service.UploadDocumentAsync(GetUserId(), file, documentName, documentType);
                return Ok(doc);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("documents/{documentId:int}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            try
            {
                await _service.DeleteDocumentAsync(GetUserId(), documentId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("The authenticated user ID is invalid.");
            }

            return userId;
        }
    }
}
