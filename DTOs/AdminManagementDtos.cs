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
}
