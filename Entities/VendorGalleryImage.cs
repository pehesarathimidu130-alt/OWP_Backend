using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorGalleryImages")]
    public class VendorGalleryImage : BaseAuditableEntity
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey(nameof(VendorId))]
        public Vendor? Vendor { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsFeatured { get; set; } = false;
    }
}
