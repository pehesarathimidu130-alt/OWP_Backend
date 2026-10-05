using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorServiceImages")]
    public class VendorServiceImage : BaseAuditableEntity
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public VendorService? Service { get; set; }

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsCover { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;
    }
}
