using Backend.Data;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace Backend.Controllers
{
    /// <summary>
    /// AdminSettingsController — handles settings actions for the currently logged-in admin.
    /// Route prefix: /api/admin/me
    /// </summary>
    [ApiController]
    [Route("api/admin/me")]
    [Authorize(Roles = "SUPER_ADMIN,ADMIN,Admin,SuperAdmin")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly IAdminManagementService _adminService;
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(
            IAdminManagementService adminService,
            INotificationService notificationService,
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<AdminSettingsController> logger)
        {
            _adminService = adminService;
            _notificationService = notificationService;
            _context = context;
            _environment = environment;
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
            
            await _notificationService.CreateAsync(
                userId.Value,
                Backend.Constants.NotificationTypes.SecurityChange,
                "Security Settings Updated",
                "Your PIN was changed successfully."
            );
            
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

            await _notificationService.CreateAsync(
                userId.Value,
                Backend.Constants.NotificationTypes.SecurityChange,
                "Security Settings Updated",
                "Your password was changed successfully."
            );

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

            await _notificationService.CreateAsync(
                userId.Value,
                Backend.Constants.NotificationTypes.ProfileUpdated,
                "Profile Updated",
                "Your admin profile information has been successfully updated."
            );

            return Ok(new { success = true, message = "Profile updated successfully." });
        }

        /// <summary>
        /// GET /api/admin/me
        /// Retrieves the current authenticated admin's profile.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(AdminProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCallerUserId();
            if (userId == null)
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });

            var admin = await _context.Admins
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (admin == null)
                return NotFound(new { detail = "Administrator profile not found." });

            return Ok(new AdminProfileResponseDto
            {
                AdminId = admin.AdminId,
                UserId = admin.UserId,
                FullName = admin.FullName,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                Email = admin.User?.Email ?? string.Empty,
                Role = admin.AccessLevel,
                Department = admin.Department,
                PhoneNumber = admin.PhoneNumber,
                ProfilePictureUrl = admin.ProfilePictureUrl,
                CreatedAt = admin.CreatedAt,
                UpdatedAt = admin.UpdatedAt
            });
        }

        /// <summary>
        /// POST /api/admin/me/photo
        /// Uploads/updates the current admin's profile photo.
        /// Reuses the vendor image upload pattern (JWT-scoped, jpg/jpeg/png/webp validation, 5MB limit).
        /// </summary>
        [HttpPost("photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            var userId = GetCallerUserId();
            if (userId == null)
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (admin == null)
                return NotFound(new { detail = "Administrator profile not found." });

            if (file == null || file.Length == 0)
                return BadRequest(new { detail = "Please provide a valid image file." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { detail = $"Unsupported file format '{extension}'. Allowed: {string.Join(", ", allowedExtensions)}" });
            }

            const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxSizeBytes)
            {
                return BadRequest(new { detail = "Image size exceeds the 5 MB limit." });
            }

            var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(rootPath, "uploads", "admin-profile");
            Directory.CreateDirectory(folder);

            // Delete existing profile photo if exists
            if (!string.IsNullOrWhiteSpace(admin.ProfilePictureUrl))
            {
                try
                {
                    var oldCleanPath = admin.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldFullPath = Path.Combine(rootPath, oldCleanPath);
                    if (System.IO.File.Exists(oldFullPath))
                    {
                        System.IO.File.Delete(oldFullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile photo: {Url}", admin.ProfilePictureUrl);
                }
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var destinationPath = Path.Combine(folder, fileName);

            await using (var stream = System.IO.File.Create(destinationPath))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/admin-profile/{fileName}";
            admin.ProfilePictureUrl = relativeUrl;
            admin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                userId.Value,
                Backend.Constants.NotificationTypes.ProfileUpdated,
                "Profile Photo Updated",
                "Your profile photo has been successfully updated."
            );

            return Ok(new
            {
                photoUrl = relativeUrl,
                message = "Profile photo updated successfully."
            });
        }

        /// <summary>
        /// DELETE /api/admin/me/photo
        /// Removes the current admin's profile photo.
        /// </summary>
        [HttpDelete("photo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemovePhoto()
        {
            var userId = GetCallerUserId();
            if (userId == null)
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (admin == null)
                return NotFound(new { detail = "Administrator profile not found." });

            if (!string.IsNullOrWhiteSpace(admin.ProfilePictureUrl))
            {
                try
                {
                    var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var cleanPath = admin.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(rootPath, cleanPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete profile photo file: {Url}", admin.ProfilePictureUrl);
                }

                admin.ProfilePictureUrl = null;
                admin.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Profile photo removed successfully." });
        }

        /// <summary>
        /// GET /api/admin/me/notifications
        /// Retrieves the current admin's notification preferences.
        /// </summary>
        [HttpGet("notifications")]
        [ProducesResponseType(typeof(AdminNotificationPreferencesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNotificationPreferences()
        {
            var userId = GetCallerUserId();
            if (userId == null)
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (admin == null)
                return NotFound(new { detail = "Administrator profile not found." });

            return Ok(new AdminNotificationPreferencesDto
            {
                NewVendorPending = admin.NotifyNewVendorPending,
                FlaggedContent = admin.NotifyFlaggedContent,
                CustomerComplaint = admin.NotifyCustomerComplaint,
                AiWorkflowApproval = admin.NotifyAiWorkflowApproval,
                WeeklySummary = admin.NotifyWeeklySummary
            });
        }

        /// <summary>
        /// PUT /api/admin/me/notifications
        /// Updates the current admin's notification preferences.
        /// </summary>
        [HttpPut("notifications")]
        [ProducesResponseType(typeof(AdminNotificationPreferencesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNotificationPreferences([FromBody] UpdateAdminNotificationPreferencesDto request)
        {
            var userId = GetCallerUserId();
            if (userId == null)
                return Unauthorized(new { detail = "Unable to identify the caller from the JWT token." });

            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (admin == null)
                return NotFound(new { detail = "Administrator profile not found." });

            if (request.NewVendorPending.HasValue)
                admin.NotifyNewVendorPending = request.NewVendorPending.Value;
            if (request.FlaggedContent.HasValue)
                admin.NotifyFlaggedContent = request.FlaggedContent.Value;
            if (request.CustomerComplaint.HasValue)
                admin.NotifyCustomerComplaint = request.CustomerComplaint.Value;
            if (request.AiWorkflowApproval.HasValue)
                admin.NotifyAiWorkflowApproval = request.AiWorkflowApproval.Value;
            if (request.WeeklySummary.HasValue)
                admin.NotifyWeeklySummary = request.WeeklySummary.Value;

            admin.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification preferences updated for Admin UserId {UserId}", userId);

            return Ok(new AdminNotificationPreferencesDto
            {
                NewVendorPending = admin.NotifyNewVendorPending,
                FlaggedContent = admin.NotifyFlaggedContent,
                CustomerComplaint = admin.NotifyCustomerComplaint,
                AiWorkflowApproval = admin.NotifyAiWorkflowApproval,
                WeeklySummary = admin.NotifyWeeklySummary
            });
        }
    }
}

