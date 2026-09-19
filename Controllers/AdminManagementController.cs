using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/admin-management")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,Admin")]
    public class AdminManagementController : ControllerBase
    {
        private readonly IAdminManagementService _adminService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminManagementController> _logger;

        public AdminManagementController(
            IAdminManagementService adminService,
            INotificationService notificationService,
            ILogger<AdminManagementController> logger)
        {
            _adminService = adminService;
            _notificationService = notificationService;
            _logger = logger;
        }

        private int? GetCallerUserId()
        {
            var claim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
                return userId;
            return null;
        }

        /// <summary>
        /// GET /api/admin-management
        /// Returns all admin users with their profile and PIN details.
        /// Protected: SUPER_ADMIN role required.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<AdminResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return Ok(admins);
        }

        /// <summary>
        /// POST /api/admin-management
        /// Creates a new admin user with auto-generated secure PIN.
        /// Protected: SUPER_ADMIN role required.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AdminResponseDto), StatusCodes.Status201Created)]
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
                var result = await _adminService.CreateAdminAsync(request);

                var currentUserId = GetCallerUserId();
                await _notificationService.CreateForAllAdminsAsync(
                    Backend.Constants.NotificationTypes.AdminAccountCreated,
                    "New Admin Account Created",
                    $"A new administrator account ({result.FullName}) has been created.",
                    currentUserId
                );

                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Admin creation failed: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Duplicate Entry"
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Admin creation error: {Message}", ex.Message);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }
    }
}
