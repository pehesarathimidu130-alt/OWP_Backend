using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class BusinessHoursItemDto
    {
        public string Day { get; set; } = string.Empty; // e.g. "Monday"
        public bool IsClosed { get; set; } = false;
        public string OpenTime { get; set; } = "09:00";
        public string CloseTime { get; set; } = "18:00";
    }

    public class SocialLinksDto
    {
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? TikTok { get; set; }
        public string? YouTube { get; set; }
        public string? Pinterest { get; set; }
        public string? Website { get; set; }
    }

    public class VendorGalleryImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string? Category { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsFeatured { get; set; }
    }

    public class VendorGalleryUpdateRequestDto
    {
        public string? Caption { get; set; }
        public string? Category { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsFeatured { get; set; }
    }

    public class VendorDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime? UploadedAt { get; set; }
    }

    public class VendorProfileResponseDto
    {
        public int VendorId { get; set; }
        public int UserId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Tagline { get; set; }
        public string? Description { get; set; }
        public string? OwnerName { get; set; }
        public string? ContactNumber { get; set; }
        public string? AltPhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? ServiceAreas { get; set; }
        public string? TravelPolicy { get; set; }
        public int? YearsInBusiness { get; set; }
        public bool IsApproved { get; set; }
        public string Status { get; set; } = "Pending";
        public string VerificationStatus { get; set; } = "Pending";
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<BusinessHoursItemDto> BusinessHours { get; set; } = new();
        public SocialLinksDto SocialLinks { get; set; } = new();
        public List<VendorGalleryImageDto> GalleryImages { get; set; } = new();
        public List<VendorDocumentDto> Documents { get; set; } = new();
    }

    public class VendorProfileUpdateDto
    {
        [Required]
        [MaxLength(150)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(250)]
        public string? Tagline { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(150)]
        public string? OwnerName { get; set; }

        [MaxLength(30)]
        public string? ContactNumber { get; set; }

        [MaxLength(30)]
        public string? AltPhoneNumber { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(30)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(500)]
        public string? ServiceAreas { get; set; }

        [MaxLength(500)]
        public string? TravelPolicy { get; set; }

        public int? YearsInBusiness { get; set; }

        public List<BusinessHoursItemDto>? BusinessHours { get; set; }

        public SocialLinksDto? SocialLinks { get; set; }
    }

    /// Customer-facing vendor profile (no documents, user account, or internal fields).
    public class PublicVendorProfileDto
    {
        public int VendorId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Tagline { get; set; }
        public string? Description { get; set; }
        public string? OwnerName { get; set; }
        public string? ContactNumber { get; set; }
        public string? AltPhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? ServiceAreas { get; set; }
        public string? TravelPolicy { get; set; }
        public int? YearsInBusiness { get; set; }
        public bool IsApproved { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string Location { get; set; } = "Sri Lanka";
        public int ReviewCount { get; set; }
        public List<BusinessHoursItemDto> BusinessHours { get; set; } = new();
        public SocialLinksDto SocialLinks { get; set; } = new();
        public List<VendorGalleryImageDto> GalleryImages { get; set; } = new();
        public List<VendorPerformanceResponseDto> Performances { get; set; } = new();
        public List<PublicVendorServiceItemDto> Services { get; set; } = new();
    }

    public class PublicVendorServiceItemDto
    {
        public int ServiceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? ShortDescription { get; set; }
        public decimal? Price { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
