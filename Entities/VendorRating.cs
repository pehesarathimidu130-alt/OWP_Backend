using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorRatings")]
    public class VendorRating : BaseAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey("VendorId")]
        public Vendor? Vendor { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }
    }
}
