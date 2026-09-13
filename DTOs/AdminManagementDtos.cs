using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Request payload for POST /api/admin/administrators.
    /// Creates a new Admin/SuperAdmin user with auto-generated 4-digit secure PIN.
    /// </summary>
    public class CreateAdminRequestDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        // Backward compatibility helper for legacy calls supplying FullName
        public string FullName
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Trim().Split(' ', 2);
                    FirstName = parts[0];
                    LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role / AccessLevel is required.")]
        public string Role { get; set; } = "Admin"; // "Admin" or "SuperAdmin"

        [MaxLength(100)]
        public string? Department { get; set; } = "Administration";

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Response payload returned when creating an admin.
    /// The raw 4-digit PIN is returned ONLY once upon creation to display to the Super Admin.
    /// </summary>
    public class CreateAdminResponseDto
    {
        public int AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string GeneratedPin { get; set; } = string.Empty; // One-time display
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response payload returned when explicitly regenerating an admin's PIN.
    /// Returns the new PIN once.
    /// </summary>
    public class RegeneratePinResponseDto
    {
        public int AdminId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NewGeneratedPin { get; set; } = string.Empty; // One-time display
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO representing an administrator in the management table.
    /// NO plaintext PIN is returned. Only a masked placeholder and status indicator.
    /// </summary>
    public class AdminListItemDto
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string AccessLevel { get; set; } = string.Empty;
        public string SecurePin { get; set; } = "••••"; // Masked placeholder
        public bool HasPinConfigured { get; set; } = true;
        public string Department { get; set; } = "Administration";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request payload for PUT /api/admin/administrators/{id}.
    /// </summary>
    public class UpdateAdminDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "AccessLevel is required.")]
        public string AccessLevel { get; set; } = "Admin"; // "Admin" or "SuperAdmin"

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Department { get; set; } = "Administration";

        public bool RegeneratePin { get; set; } = false;
    }

    /// <summary>
    /// Response payload for PUT /api/admin/administrators/{id}.
    /// </summary>
    public class UpdateAdminResponseDto
    {
        public int AdminId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string AccessLevel { get; set; } = string.Empty;
        public string SecurePin { get; set; } = "••••";
        public string Department { get; set; } = "Administration";
        public bool IsActive { get; set; }
        public string? NewPin { get; set; } // Only populated if RegeneratePin was requested
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Stat counters for the Super Admin dashboard cards.
    /// </summary>
    public class AdminMetricsDto
    {
        public int TotalAdmins { get; set; }
        public int ActiveAdmins { get; set; }
        public int SuperAdmins { get; set; }
    }

    /// <summary>
    /// Legacy response payload returned when listing or creating admins in /api/admin-management.
    /// </summary>
    public class AdminResponseDto
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string SecurePin { get; set; } = "••••";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── PIN Management DTOs (used by /api/admin/me/pin/verify and /generate) ──

    public class VerifyPinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;
    }

    public class GeneratePinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;
    }

    public class SavePinRequestDto
    {
        [Required(ErrorMessage = "Current PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Current PIN must be exactly 4 digits.")]
        public string CurrentPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "New PIN is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "New PIN must be exactly 4 digits.")]
        public string NewPin { get; set; } = string.Empty;
    }

    public class GeneratePinResponseDto
    {
        public string Pin { get; set; } = string.Empty;
        public bool AlreadyInUse { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateAdminProfileRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }
}
