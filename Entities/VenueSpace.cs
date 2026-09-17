using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VenueSpaces")]
    public class VenueSpace : BaseAuditableEntity
    {
        [Key]
        public int VenueSpaceId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public string SpaceName { get; set; } = string.Empty;

        [Required]
        public string SpaceType { get; set; } = string.Empty;

        public int? SeatedCapacity { get; set; }
        public int? FloatingCapacity { get; set; }

        public bool? AirConditioned { get; set; }

        [MaxLength(2000)]
        public string? KeyFeatures { get; set; }

        public VendorService? VendorService { get; set; }
    }
}
