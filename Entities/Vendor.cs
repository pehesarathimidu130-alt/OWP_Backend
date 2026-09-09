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
    }
}