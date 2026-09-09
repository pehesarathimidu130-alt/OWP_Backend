using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Request payload for POST /api/auth/login.
    /// Matches the frontend LoginPage.jsx form state:
    ///   { email, password, isAdmin, pin? }
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// When true, the backend validates the user as an Admin
        /// and requires the SecurePin field.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// 4-digit PIN required only when IsAdmin is true.
        /// The frontend joins the 4-cell PIN array into a single string.
        /// </summary>
        [MaxLength(4)]
        public string? Pin { get; set; }
    }

    /// <summary>
    /// Successful authentication response returned to the client.
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
