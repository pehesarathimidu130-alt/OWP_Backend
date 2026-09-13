using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    /// <summary>
    /// AdminsController — provides Super Admin CRUD endpoints for administrator accounts,
    /// dynamic PIN generation, role assignment, and management metrics.
    /// Route prefix: /api/admin
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "SuperAdmin,SUPER_ADMIN")]
    public class AdminsController : ControllerBase
    {
        private readonly IAdminManagementService _adminService;
        private readonly ILogger<AdminsController> _logger;

        public AdminsController(
            IAdminManagementService adminService,
            ILogger<AdminsController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// POST /api/admin/administrators
        /// Creates a new administrator with auto-generated secure PIN.
        /// </summary>
        [HttpPost("administrators")]
        [ProducesResponseType(typeof(CreateAdminResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _adminService.CreateAdministratorAsync(request);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("CreateAdmin failed: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Duplicate Email"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAdmin internal error: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }

        /// <summary>
        /// GET /api/admin/administrators
        /// Returns all administrators with optional search, role, and status filters.
        /// </summary>
        [HttpGet("administrators")]
        [ProducesResponseType(typeof(List<AdminListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAdmins(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null)
        {
            var admins = await _adminService.GetAdministratorsAsync(search, role, status);
            return Ok(admins);
        }

        /// <summary>
        /// GET /api/admin/metrics
        /// Returns system admin dashboard metrics { totalAdmins, activeAdmins, superAdmins }.
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(AdminMetricsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetrics()
        {
            var metrics = await _adminService.GetAdminMetricsAsync();
            return Ok(metrics);
        }

        /// <summary>
        /// GET /api/admin/administrators/{id}
        /// Retrieves profile details for a single administrator.
        /// </summary>
        [HttpGet("administrators/{id}")]
        [ProducesResponseType(typeof(AdminListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdminById([FromRoute] int id)
        {
            var admin = await _adminService.GetAdministratorByIdAsync(id);
            if (admin == null)
            {
                return NotFound(new { message = $"Administrator with ID {id} not found." });
            }
            return Ok(admin);
        }

        /// <summary>
        /// PUT /api/admin/administrators/{id}
        /// Updates an administrator's profile, access level, active status, department, and optionally regenerates PIN.
        /// </summary>
        [HttpPut("administrators/{id}")]
        [ProducesResponseType(typeof(UpdateAdminResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAdmin([FromRoute] int id, [FromBody] UpdateAdminDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _adminService.UpdateAdministratorAsync(id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAdmin error for ID {Id}: {Message}", id, ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }

        /// <summary>
        /// POST /api/admin/administrators/{id}/regenerate-pin
        /// Explicitly regenerates an administrator's PIN, updates SecurePinHash in the database,
        /// and returns the newly generated PIN once.
        /// </summary>
        [HttpPost("administrators/{id}/regenerate-pin")]
        [ProducesResponseType(typeof(RegeneratePinResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegeneratePin([FromRoute] int id)
        {
            try
            {
                var result = await _adminService.RegeneratePinAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegeneratePin error for ID {Id}: {Message}", id, ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }

        /// <summary>
        /// DELETE /api/admin/administrators/{id}
        /// Permanently hard-deletes an administrator and their linked User account. Blocks self-deletion of active Super Admin.
        /// </summary>
        [HttpDelete("administrators/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdmin([FromRoute] int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Caller identity could not be verified." });
            }

            if (id == currentUserId.Value)
            {
                return BadRequest(new { message = "You cannot delete your own active Super Admin account." });
            }

            try
            {
                var success = await _adminService.DeleteAdministratorAsync(id, currentUserId.Value);
                if (!success)
                {
                    return NotFound(new { message = $"Administrator with ID {id} not found." });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAdmin error for ID {Id}: {Message}", id, ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }
    }
}
