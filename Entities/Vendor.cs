using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("Vendors")]
    public class Vendor : BaseAuditableEntity
    {
        [Key]
        public int VendorId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(150)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        // Critical for Admin Listing & Vendor Review workflows
        public bool IsApproved { get; set; } = false; 
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(250)]
        public string? Tagline { get; set; }

        [MaxLength(150)]
        public string? OwnerName { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? AltPhoneNumber { get; set; }

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

        [MaxLength(2000)]
        public string? BusinessHoursJson { get; set; }

        [MaxLength(2000)]
        public string? SocialLinksJson { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public int? YearsInBusiness { get; set; }

        [MaxLength(50)]
        public string VerificationStatus { get; set; } = "Pending";

        public ICollection<VendorGalleryImage> GalleryImages { get; set; } = new List<VendorGalleryImage>();
        public ICollection<VendorDocument> Documents { get; set; } = new List<VendorDocument>();
    }
}