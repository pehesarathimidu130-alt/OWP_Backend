using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/vendor-content")]
    [Authorize(Roles = "Vendor")]
    public class VendorContentController : ControllerBase
    {
        private readonly IVendorContentService _service;

        public VendorContentController(IVendorContentService service)
        {
            _service = service;
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                return Ok(await _service.GetServicesAsync(GetUserId()));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("services/{serviceId:int}")]
        public async Task<IActionResult> GetService(int serviceId)
        {
            try
            {
                return Ok(await _service.GetServiceByIdAsync(GetUserId(), serviceId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("services")]
        public async Task<IActionResult> AddService([FromBody] VendorServiceRequestDto request)
        {
            try
            {
                var result = await _service.AddServiceAsync(GetUserId(), request);
                return CreatedAtAction(nameof(GetService), new { serviceId = result.ServiceId }, result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpPut("services/{serviceId:int}")]
        public async Task<IActionResult> UpdateService(int serviceId, [FromBody] VendorServiceRequestDto request)
        {
            try
            {
                return Ok(await _service.UpdateServiceAsync(GetUserId(), serviceId, request));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpDelete("services/{serviceId:int}")]
        public async Task<IActionResult> DeleteService(int serviceId)
        {
            try
            {
                await _service.DeleteServiceAsync(GetUserId(), serviceId);
                return NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpPost("services/{serviceId:int}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadServiceImage(int serviceId, IFormFile file, [FromForm] bool isCover = false)
        {
            try
            {
                var result = await _service.UploadServiceImageAsync(GetUserId(), serviceId, file, isCover);
                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpDelete("services/{serviceId:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteServiceImage(int serviceId, int imageId)
        {
            try
            {
                await _service.DeleteServiceImageAsync(GetUserId(), serviceId, imageId);
                return NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpGet("performances")]
        public async Task<IActionResult> GetPerformances() => Ok(await _service.GetPerformancesAsync(GetUserId()));

        [HttpPost("performances")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddPerformance([FromForm] VendorPerformanceRequestDto request, IFormFile? photo)
        {
            try
            {
                return Ok(await _service.AddPerformanceAsync(GetUserId(), request, photo));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("performances/{performanceId:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePerformance(int performanceId, [FromForm] VendorPerformanceRequestDto request, IFormFile? photo)
        {
            try
            {
                return Ok(await _service.UpdatePerformanceAsync(GetUserId(), performanceId, request, photo));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpDelete("performances/{performanceId:int}")]
        public async Task<IActionResult> DeletePerformance(int performanceId)
        {
            try
            {
                await _service.DeletePerformanceAsync(GetUserId(), performanceId);
                return NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
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
