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
        public async Task<IActionResult> GetServices() => Ok(await _service.GetServicesAsync(GetUserId()));

        [HttpPost("services")]
        public async Task<IActionResult> AddService(VendorServiceRequestDto request)
        {
            try
            {
                return Ok(await _service.AddServiceAsync(GetUserId(), request));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("services/{serviceId:int}")]
        public async Task<IActionResult> UpdateService(int serviceId, VendorServiceRequestDto request) => Ok(await _service.UpdateServiceAsync(GetUserId(), serviceId, request));

        [HttpDelete("services/{serviceId:int}")]
        public async Task<IActionResult> DeleteService(int serviceId)
        {
            await _service.DeleteServiceAsync(GetUserId(), serviceId);
            return NoContent();
        }

        [HttpGet("performances")]
        public async Task<IActionResult> GetPerformances() => Ok(await _service.GetPerformancesAsync(GetUserId()));

        [HttpPost("performances")]
        public async Task<IActionResult> AddPerformance([FromForm] VendorPerformanceRequestDto request, [FromForm] IFormFile? photo)
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
        public async Task<IActionResult> UpdatePerformance(int performanceId, [FromForm] VendorPerformanceRequestDto request, [FromForm] IFormFile? photo) => Ok(await _service.UpdatePerformanceAsync(GetUserId(), performanceId, request, photo));

        [HttpDelete("performances/{performanceId:int}")]
        public async Task<IActionResult> DeletePerformance(int performanceId)
        {
            await _service.DeletePerformanceAsync(GetUserId(), performanceId);
            return NoContent();
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications() => Ok(await _service.GetNotificationsAsync(GetUserId()));

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
