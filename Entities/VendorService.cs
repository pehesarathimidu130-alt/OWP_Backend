using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorServices")]
    public class VendorService : BaseAuditableEntity
    {
        public static readonly string[] AllowedCategories =
        {
            "Photography",
            "Decorations",
            "Catering",
            "Music"
        };

        [Key]
        public int ServiceId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey(nameof(VendorId))]
        public Vendor? Vendor { get; set; }

        [Required, MaxLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }
    }
}
