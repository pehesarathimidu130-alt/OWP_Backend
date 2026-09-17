using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorServices")]
    public class VendorService : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [ForeignKey(nameof(VendorId))]
        public Vendor? Vendor { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required, MaxLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ShortDescription { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Required]
        public bool IsPriceOnRequest { get; set; } = false;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Draft";

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public ICollection<VendorServiceImage> Images { get; set; } = new List<VendorServiceImage>();
        public ICollection<VenueSpace> VenueSpaces { get; set; } = new List<VenueSpace>();
        
        public CateringDetails? CateringDetails { get; set; }
        public DecorationsDetails? DecorationsDetails { get; set; }
        public HotelVenueDetails? HotelVenueDetails { get; set; }
        public MusicDetails? MusicDetails { get; set; }
        public PhotographyDetails? PhotographyDetails { get; set; }
    }
}
