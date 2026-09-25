using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs
{
    public class CustomerRegisterRequestDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? NameAlias
        {
            get => FullName;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(FullName))
                {
                    FullName = value;
                }
            }
        }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumberAlias
        {
            get => Phone;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Phone))
                {
                    Phone = value;
                }
            }
        }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class CustomerLoginRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class CustomerAuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CustomerForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }

    public class CustomerResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required.")]
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
