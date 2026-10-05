using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorPerformances")]
    public class VendorPerformance : BaseAuditableEntity
    {
        [Key]
        public int PerformanceId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey(nameof(VendorId))]
        public Vendor? Vendor { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(2000)]
        public string? CustomerFeedback { get; set; }

        public DateTime? EventDate { get; set; }
    }
}
