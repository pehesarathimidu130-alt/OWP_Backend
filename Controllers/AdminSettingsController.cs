using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    /// <summary>
    /// AdminSettingsController — handles settings actions for the currently logged-in admin.
    /// Route prefix: /api/admin/me
    ///
    /// Endpoints implemented here:
    ///   POST /api/admin/me/pin/verify   — verify current PIN without changing it
    ///   POST /api/admin/me/pin/generate — generate a candidate PIN without saving it to DB
    ///   POST /api/admin/me/pin/save     — save confirmed new PIN to DB
    ///
    /// UserId is read from the JWT claim (ClaimTypes.NameIdentifier), which is set by
    /// AuthService.GenerateJwtToken() using the User.UserId from the database.
    /// </summary>
    [ApiController]
    [Route("api/admin/me")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,Admin")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly IAdminManagementService _adminService;
        private readonly ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(
            IAdminManagementService adminService,
            ILogger<AdminSettingsController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // ── Helper: extract UserId from the JWT NameIdentifier claim ──────────
        private int? GetCallerUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var userId))
                return userId;
            return null;
        }

        /// <summary>
        /// POST /api/admin/me/pin/verify
        /// Verifies the caller's current PIN without changing it.
        /// Returns 200 OK if the PIN is correct.
        /// Returns 400 Bad Request if the PIN is wrong or the admin record is not found.
        /// </summary>
        [HttpPost("pin/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyPin([FromBody] VerifyPinRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCallerUserId();
            if (userId == null)
            {
                _logger.LogWarning("VerifyPin: could not resolve UserId from JWT claims");
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });
            }

            var isCorrect = await _adminService.VerifyPinAsync(userId.Value, request.CurrentPin);

            if (!isCorrect)
            {
                _logger.LogWarning("VerifyPin: incorrect PIN for UserId {UserId}", userId);
                return Problem(
                    detail: "Incorrect PIN. Please check and try again.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Incorrect PIN"
                );
            }

            _logger.LogInformation("VerifyPin: PIN verified successfully for UserId {UserId}", userId);
            return Ok(new { verified = true });
        }

        /// <summary>
        /// POST /api/admin/me/pin/generate
        /// Re-confirms the caller's identity via their current PIN, then generates a candidate
        /// 4-digit PIN, checks it is not already in use by any other admin, and returns it.
        /// DOES NOT save the PIN to the database yet.
        ///
        /// Response shape: { pin: "7392", alreadyInUse: false }
        ///   alreadyInUse = true means all generated candidates collided — client should retry.
        /// </summary>
        [HttpPost("pin/generate")]
        [ProducesResponseType(typeof(GeneratePinResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GeneratePin([FromBody] GeneratePinRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCallerUserId();
            if (userId == null)
            {
                _logger.LogWarning("GeneratePin: could not resolve UserId from JWT claims");
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });
            }

            // Re-confirm identity before generating — same check as login
            var isCorrect = await _adminService.VerifyPinAsync(userId.Value, request.CurrentPin);
            if (!isCorrect)
            {
                _logger.LogWarning("GeneratePin: incorrect PIN for UserId {UserId}", userId);
                return Problem(
                    detail: "Incorrect current PIN. Cannot generate a new PIN.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Incorrect PIN"
                );
            }

            try
            {
                var result = await _adminService.GenerateCandidatePinAsync(userId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "GeneratePin error for UserId {UserId}", userId);
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Server Error"
                );
            }
        }

        /// <summary>
        /// POST /api/admin/me/pin/save
        /// Saves the selected new PIN to the database after verifying the current PIN.
        /// </summary>
        [HttpPost("pin/save")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SavePin([FromBody] SavePinRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCallerUserId();
            if (userId == null)
            {
                _logger.LogWarning("SavePin: could not resolve UserId from JWT claims");
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });
            }

            var success = await _adminService.SaveNewPinAsync(userId.Value, request.CurrentPin, request.NewPin);
            if (!success)
            {
                _logger.LogWarning("SavePin: incorrect current PIN during save for UserId {UserId}", userId);
                return Problem(
                    detail: "Incorrect current PIN. Could not save new PIN.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Incorrect PIN"
                );
            }

            _logger.LogInformation("SavePin: PIN saved successfully for UserId {UserId}", userId);
            return Ok(new { success = true, message = "PIN changed successfully." });
        }

        /// <summary>
        /// POST /api/admin/me/change-password
        /// Verifies current password and updates to new BCrypt hashed password.
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCallerUserId();
            if (userId == null)
            {
                _logger.LogWarning("ChangePassword: could not resolve UserId from JWT claims");
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });
            }

            var success = await _adminService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            if (!success)
            {
                return Problem(
                    detail: "Incorrect current password. Please check and try again.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Password Change Failed"
                );
            }

            return Ok(new { success = true, message = "Password changed successfully." });
        }

        /// <summary>
        /// PUT /api/admin/me
        /// Updates the current admin's profile name.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCallerUserId();
            if (userId == null)
            {
                _logger.LogWarning("UpdateProfile: could not resolve UserId from JWT claims");
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });
            }

            var success = await _adminService.UpdateProfileAsync(userId.Value, request.FullName);
            if (!success)
            {
                return Problem(
                    detail: "Failed to update profile.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Update Profile Failed"
                );
            }

            return Ok(new { success = true, message = "Profile updated successfully." });
        }
    }
}

