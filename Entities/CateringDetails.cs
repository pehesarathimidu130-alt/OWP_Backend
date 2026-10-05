using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("CateringDetails")]
    public class CateringDetails : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        public string? ServiceStyle { get; set; }
        public string[]? Cuisines { get; set; }
        public string[]? DietaryOptions { get; set; }
        public int? MinGuests { get; set; }
        public int? MaxGuests { get; set; }
        public string? PricePerHead { get; set; }
        
        public bool? WaitstaffIncluded { get; set; }
        public bool? GlasswareIncluded { get; set; }
        public string? CrockeryCutlery { get; set; }
        public bool? ChafingDishesIncluded { get; set; }
        public bool? FurnitureRentalAvailable { get; set; }
        public bool? SetupTeardownIncluded { get; set; }
        
        public string? OutstationCatering { get; set; }
        public string? KitchenRequirement { get; set; }
        
        public bool TastingAvailable { get; set; } = false;
        public string? TastingPolicy { get; set; }
    }
}
