using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/customer/profile")]
    [Authorize(Roles = "Customer")]
    public class CustomerProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Backend.Services.IFileStorage _fileStorage;
        private readonly ILogger<CustomerProfileController> _logger;

        public CustomerProfileController(
            AppDbContext context,
            Backend.Services.IFileStorage fileStorage,
            ILogger<CustomerProfileController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        private int GetUserId()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Caller is not authorized as a Customer.");
            }

            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user identity in token.");
            }
            return userId;
        }

        /// <summary>
        /// GET /api/customer/profile
        /// Retrieves the authenticated customer's profile and quick stats.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(CustomerProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "User record not found." });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                // Auto-provision Customer record if missing for a valid Customer role user
                var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                customer = new Customer
                {
                    UserId = user.UserId,
                    FirstName = nameParts.Length > 0 ? nameParts[0] : "Customer",
                    LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : ""
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var favCount = await _context.CustomerFavorites.CountAsync(f => f.CustomerId == customer.CustomerId || f.UserId == userId);
            var inqCount = await _context.VendorInquiries.CountAsync(i => i.CustomerId == customer.CustomerId || i.UserId == userId);

            var profile = new CustomerProfileResponseDto
            {
                CustomerId = customer.CustomerId,
                UserId = user.UserId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? $"{customer.FirstName} {customer.LastName}".Trim() : user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                FavoritesCount = favCount,
                InquiriesCount = inqCount,
                ProfilePhotoUrl = customer.ProfilePhotoUrl
            };

            return Ok(profile);
        }

        /// <summary>
        /// PUT /api/customer/profile
        /// Updates the authenticated customer's name and contact details.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(CustomerProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                customer = new Customer
                {
                    UserId = user.UserId,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim()
                };
                _context.Customers.Add(customer);
            }
            else
            {
                customer.FirstName = request.FirstName.Trim();
                customer.LastName = request.LastName.Trim();
                customer.UpdatedAt = DateTime.UtcNow;
            }

            user.FullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var favCount = await _context.CustomerFavorites.CountAsync(f => f.CustomerId == customer.CustomerId || f.UserId == userId);
            var inqCount = await _context.VendorInquiries.CountAsync(i => i.CustomerId == customer.CustomerId || i.UserId == userId);

            var profile = new CustomerProfileResponseDto
            {
                CustomerId = customer.CustomerId,
                UserId = user.UserId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                FavoritesCount = favCount,
                InquiriesCount = inqCount,
                ProfilePhotoUrl = customer.ProfilePhotoUrl
            };

            _logger.LogInformation("Profile updated successfully for customer {CustomerId}", customer.CustomerId);

            return Ok(profile);
        }

        /// <summary>
        /// POST /api/customer/profile/change-password OR /api/auth/customer/change-password
        /// Allows a logged-in customer to update their password.
        /// </summary>
        [HttpPost("change-password")]
        [HttpPost("/api/auth/customer/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] CustomerChangePasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Problem(
                    detail: "The current password you entered is incorrect.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Authentication Error"
                );
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password successfully changed for UserId {UserId}", user.UserId);

            return Ok(new { message = "Password updated successfully." });
        }

        /// <summary>
        /// POST /api/customer/profile/change-email
        /// Allows a logged-in customer to update their email address.
        /// Requires current password confirmation for security.
        /// </summary>
        [HttpPost("change-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Verify current password before allowing email change
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Problem(
                    detail: "The password you entered is incorrect.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Authentication Error"
                );
            }

            // Check the new email is not already in use
            var normalizedNew = request.NewEmail.Trim().ToLower();
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedNew && u.UserId != userId);
            if (emailExists)
            {
                return Problem(
                    detail: "An account with this email address already exists.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Email Conflict"
                );
            }

            user.Email = request.NewEmail.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email changed successfully for UserId {UserId}", user.UserId);

            return Ok(new { message = "Email updated successfully.", email = user.Email });
        }

        /// <summary>
        /// POST /api/customer/profile/photo
        /// Uploads and updates the authenticated customer's profile photo.
        /// </summary>
        [HttpPost("photo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
        {
            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { detail = "Please select an image file to upload." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { detail = $"Unsupported file format '{extension}'. Allowed: {string.Join(", ", allowedExtensions)}" });
            }

            const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxSizeBytes)
            {
                return BadRequest(new { detail = "Image size exceeds the 5 MB limit." });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }
                var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                customer = new Customer
                {
                    UserId = user.UserId,
                    FirstName = nameParts.Length > 0 ? nameParts[0] : "Customer",
                    LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : ""
                };
                _context.Customers.Add(customer);
            }

            // Delete existing profile photo if exists
            if (!string.IsNullOrWhiteSpace(customer.ProfilePhotoUrl))
            {
                await _fileStorage.DeleteAsync(customer.ProfilePhotoUrl);
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var relativeUrl = await _fileStorage.SaveAsync(file, "customer-profile", fileName, file.ContentType);
            customer.ProfilePhotoUrl = relativeUrl;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile photo updated for customer {CustomerId}", customer.CustomerId);

            return Ok(new
            {
                profilePhotoUrl = relativeUrl,
                photoUrl = relativeUrl,
                message = "Profile photo updated successfully."
            });
        }

        /// <summary>
        /// GET /api/customer/notification-preferences
        /// Gets notification preferences for the authenticated customer. Defaults to true if not set.
        /// </summary>
        [HttpGet("/api/customer/notification-preferences")]
        [ProducesResponseType(typeof(CustomerNotificationPreferencesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotificationPreferences()
        {
            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                return Ok(new CustomerNotificationPreferencesResponseDto
                {
                    CustomerId = 0,
                    InquiryUpdates = true,
                    PriceChanges = true
                });
            }

            var pref = await _context.CustomerNotificationPreferences.FirstOrDefaultAsync(p => p.CustomerId == customer.CustomerId);
            if (pref == null)
            {
                return Ok(new CustomerNotificationPreferencesResponseDto
                {
                    CustomerId = customer.CustomerId,
                    InquiryUpdates = true,
                    PriceChanges = true
                });
            }

            return Ok(new CustomerNotificationPreferencesResponseDto
            {
                CustomerId = pref.CustomerId,
                InquiryUpdates = pref.InquiryUpdates,
                PriceChanges = pref.PriceChanges
            });
        }

        /// <summary>
        /// PUT /api/customer/notification-preferences
        /// Updates notification preferences for the authenticated customer.
        /// </summary>
        [HttpPut("/api/customer/notification-preferences")]
        [ProducesResponseType(typeof(CustomerNotificationPreferencesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateNotificationPreferences([FromBody] UpdateCustomerNotificationPreferencesRequestDto request)
        {
            int userId;
            try
            {
                userId = GetUserId();
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }
                var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                customer = new Customer
                {
                    UserId = user.UserId,
                    FirstName = nameParts.Length > 0 ? nameParts[0] : "Customer",
                    LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : ""
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var pref = await _context.CustomerNotificationPreferences.FirstOrDefaultAsync(p => p.CustomerId == customer.CustomerId);
            if (pref == null)
            {
                pref = new CustomerNotificationPreferences
                {
                    CustomerId = customer.CustomerId,
                    InquiryUpdates = request.InquiryUpdates,
                    PriceChanges = request.PriceChanges
                };
                _context.CustomerNotificationPreferences.Add(pref);
            }
            else
            {
                pref.InquiryUpdates = request.InquiryUpdates;
                pref.PriceChanges = request.PriceChanges;
                pref.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new CustomerNotificationPreferencesResponseDto
            {
                CustomerId = pref.CustomerId,
                InquiryUpdates = pref.InquiryUpdates,
                PriceChanges = pref.PriceChanges
            });
        }
    }
}
