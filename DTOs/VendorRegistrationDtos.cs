using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Request payload for POST /api/VendorRegistration/register.
    /// DataAnnotations mirror the spec validation rules; the service adds
    /// cross-field and database checks (email uniqueness, category exists, etc.).
    /// </summary>
    public class VendorRegistrationRequest
    {
        // ── Step 1: Account ──

        [Required(ErrorMessage = "Full name is required.")]
        [MinLength(2, ErrorMessage = "Full name must be at least 2 characters.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Required unless googleIdToken is provided.
        /// </summary>
        [MaxLength(128)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// If present, the backend will reject with GOOGLE_NOT_ENABLED until Phase 3.
        /// </summary>
        public string? GoogleIdToken { get; set; }

        // ── Step 2: Business ──

        [Required(ErrorMessage = "Business name is required.")]
        [MinLength(2, ErrorMessage = "Business name must be at least 2 characters.")]
        [MaxLength(150, ErrorMessage = "Business name cannot exceed 150 characters.")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business type is required.")]
        [MaxLength(50)]
        public string BusinessType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "Tagline cannot exceed 250 characters.")]
        public string? Tagline { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(50, ErrorMessage = "Description must be at least 50 characters.")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(0, 80, ErrorMessage = "Years in business must be between 0 and 80.")]
        public int? YearsInBusiness { get; set; }

        [MaxLength(50, ErrorMessage = "Business registration number cannot exceed 50 characters.")]
        public string? BusinessRegistrationNumber { get; set; }

        // ── Step 3: Contact & Location ──

        [Required(ErrorMessage = "Business email is required.")]
        [EmailAddress(ErrorMessage = "A valid business email address is required.")]
        [MaxLength(255, ErrorMessage = "Business email cannot exceed 255 characters.")]
        public string BusinessEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [MaxLength(30)]
        public string ContactNumber { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? AltPhoneNumber { get; set; }

        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "District is required.")]
        [MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "At least one service area is required.")]
        [MinLength(1, ErrorMessage = "At least one service area is required.")]
        public string[] ServiceAreas { get; set; } = Array.Empty<string>();

        // ── Step 4: Terms ──

        [Required(ErrorMessage = "You must accept the terms and conditions.")]
        public bool AcceptTerms { get; set; }
    }

    /// <summary>
    /// Response for GET /api/VendorRegistration/options.
    /// </summary>
    public class RegistrationOptionsResponse
    {
        public List<string> Categories { get; set; } = new();
        public List<string> Districts { get; set; } = new();
        public List<BusinessTypeOption> BusinessTypes { get; set; } = new();
    }

    public class BusinessTypeOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
