using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("HotelVenueDetails")]
    public class HotelVenueDetails : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        public string? VenueType { get; set; }
        public string? VenueSetting { get; set; }
        public string? IndoorOutdoor { get; set; }
        
        public string? ParkingCapacity { get; set; }
        public string? ParkingType { get; set; }
        public string? ValetParking { get; set; }
        
        public string? Wifi { get; set; }
        public string? WheelchairAccessible { get; set; }
        public bool? HasAirConditioning { get; set; }
        public bool? HasBackupGenerator { get; set; }
        public bool? HasElevator { get; set; }
        public bool? HasGuestDropOff { get; set; }
        public bool? HasVendorLoadingAccess { get; set; }
        
        public bool HasCeremony { get; set; } = false;
        public string? CeremonyLocation { get; set; }
        public string? OutdoorCeremonyCapacity { get; set; }
        public bool? SeparateCeremonyReceptionSpaces { get; set; }
        
        public bool HasCatering { get; set; } = false;
        public string? CateringProvidedBy { get; set; }
        public string? OutsideFoodAllowed { get; set; }
        public string? KitchenFacility { get; set; }
        public string[]? CuisineOptions { get; set; }
        public bool? BuffetAvailable { get; set; }
        public bool? PlatedDinnerAvailable { get; set; }
        public bool? CustomMenuAvailable { get; set; }
        public bool? CakeCuttingAllowed { get; set; }
        
        public bool HasBeverages { get; set; } = false;
        public string? BeverageService { get; set; }
        public string? BarFacility { get; set; }
        public string? OutsideBeveragesAllowed { get; set; }
        
        public bool HasAccommodation { get; set; } = false;
        public string? NumberOfGuestRooms { get; set; }
        public string? ComplimentaryBridalSuite { get; set; }
        public string[]? RoomTypes { get; set; }
        public bool? BridalSuiteAvailable { get; set; }
        public bool? GuestAccommodationAvailable { get; set; }
        public bool? OnSiteAccommodation { get; set; }
        
        public bool HasEntertainment { get; set; } = false;
        public string? DjAllowed { get; set; }
        public string? LiveBandAllowed { get; set; }
        public string? MaxMusicEndTime { get; set; }
        public string? ProjectorScreen { get; set; }
        public bool? TraditionalMusicAllowed { get; set; }
        
        public bool HasDecoration { get; set; } = false;
        public string? DecorationPolicy { get; set; }
        public string? TableDecoration { get; set; }
        public string? LightingDecoration { get; set; }
        public string? OutsideDecoratorAllowed { get; set; }
        public bool? BasicDecorationIncluded { get; set; }
        public bool? FloralDecorationAvailable { get; set; }
        public bool? StageDecorationAvailable { get; set; }
        
        public bool HasPhotographyPolicy { get; set; } = false;
        public bool? PhotographyAllowed { get; set; }
        public string? ExternalPhotographerAllowed { get; set; }
        public string? PreWeddingShootAllowed { get; set; }
        public string[]? PhotographyLocations { get; set; }
        
        public bool HasPolicies { get; set; } = false;
        public bool? DepositRequired { get; set; }
        public string? DepositAmount { get; set; }
        public int? MinimumGuestCount { get; set; }
        public string? MinimumBookingDuration { get; set; }
        public string? CancellationPolicy { get; set; }
        public string? OutsideVendorRestrictions { get; set; }
        public string? AdditionalCharges { get; set; }
    }
}
