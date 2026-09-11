using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Request payload for POST /api/admin-management.
    /// Creates a new Admin user with auto-generated 4-digit secure PIN.
    /// </summary>
    public class CreateAdminRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response payload returned when listing or creating admins.
    /// </summary>
    public class AdminResponseDto
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string SecurePin { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── PIN Management DTOs (used by /api/admin/me/pin/verify and /generate) ──

    /// <summary>
    /// Request for POST /api/admin/me/pin/verify.
    /// Checks the submitted PIN against the caller's stored PIN.
    /// </summary>
    public class VerifyPinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for POST /api/admin/me/pin/generate.
    /// Re-confirms identity via the current PIN before issuing a new one.
    /// </summary>
    public class GeneratePinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for POST /api/admin/me/pin/save.
    /// The user has reviewed the generated PIN and confirmed it.
    /// CurrentPin is the original PIN the user typed at the start (for final re-verification).
    /// NewPin is the candidate returned by the generate endpoint.
    /// </summary>
    public class SavePinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Current PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "New PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "New PIN must be exactly 4 digits.")]
        public string NewPin { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response for POST /api/admin/me/pin/generate.
    /// Returns the candidate PIN in plaintext.
    /// AlreadyInUse = true means all generated candidates collided — client should retry.
    /// The PIN is NOT yet saved when this response is returned.
    /// </summary>
    public class GeneratePinResponseDto
    {
        public string Pin { get; set; } = string.Empty;
        public bool AlreadyInUse { get; set; }
    }

    /// <summary>
    /// Request for POST /api/admin/me/change-password.
    /// </summary>
    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for PUT /api/admin/me.
    /// </summary>
    public class UpdateAdminProfileRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }
}

