using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("PhotographyDetails")]
    public class PhotographyDetails : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        public string? ShootingStyle { get; set; }
        public string? HoursOfCoverage { get; set; }
        public string[]? IncludedServices { get; set; }
        public string? PhotosDelivered { get; set; }
        public string? DeliveryTimeframe { get; set; }
        public bool? RawFilesIncluded { get; set; }
        public bool? DigitalGalleryIncluded { get; set; }
        
        public bool AlbumIncluded { get; set; } = false;
        public string? AlbumType { get; set; }
        public string? AlbumPages { get; set; }
        
        public string? PhotographerCount { get; set; }
        public bool? DroneAllowed { get; set; }
        public bool? BackupGear { get; set; }
        
        public bool VideographyIncluded { get; set; } = false;
        public string? VideographerCount { get; set; }
        public string? VideoLength { get; set; }
        public string[]? VideoDeliverables { get; set; }
        
        public string? TravelOutsideColombo { get; set; }
        public bool? OutstationAccommodationRequired { get; set; }
        
        public bool DepositRequired { get; set; } = false;
        public string? DepositAmount { get; set; }
        public string? CancellationPolicy { get; set; }
    }
}
