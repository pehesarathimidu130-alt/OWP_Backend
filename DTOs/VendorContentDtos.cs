using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.DTOs
{
    public class VendorServiceRequestDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }
        public int? CategoryId { get; set; }

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? FullDescription { get; set; }

        public decimal? Price { get; set; }
        public bool PriceOnRequest { get; set; } = false;

        [MaxLength(30)]
        public string Status { get; set; } = "Draft";

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public JsonElement? Details { get; set; }

        public List<VenueSpaceDto>? Spaces { get; set; }
        public HotelVenueDetailsDto? HotelVenueDetails { get; set; }
        public PhotographyDetailsDto? PhotographyDetails { get; set; }
        public MusicDetailsDto? MusicDetails { get; set; }
        public DecorationsDetailsDto? DecorationsDetails { get; set; }
        public CateringDetailsDto? CateringDetails { get; set; }
    }

    public class VendorServiceResponseDto
    {
        public int Id => ServiceId;
        public int ServiceId { get; set; }
        public int VendorId { get; set; }
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FullDescription { get; set; }
        public decimal? Price { get; set; }
        public bool PriceOnRequest { get; set; }
        public string Status { get; set; } = "Draft";
        public string? CoverImageUrl { get; set; }
        public List<VendorServiceImageDto> Images { get; set; } = new();
        public int Views { get; set; }
        public int Inquiries { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public object? Details { get; set; }

        public List<VenueSpaceDto>? Spaces { get; set; }
        public HotelVenueDetailsDto? HotelVenueDetails { get; set; }
        public PhotographyDetailsDto? PhotographyDetails { get; set; }
        public MusicDetailsDto? MusicDetails { get; set; }
        public DecorationsDetailsDto? DecorationsDetails { get; set; }
        public CateringDetailsDto? CateringDetails { get; set; }
    }

    public class VendorServiceImageDto
    {
        public int ImageId { get; set; }
        public int ServiceId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsCover { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VenueSpaceDto
    {
        public int? SpaceId { get; set; }
        public int? VenueSpaceId { get => SpaceId; set => SpaceId = value; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public int? CapacitySeated { get; set; }
        public int? CapacityFloating { get; set; }
        public bool IsAirConditioned { get; set; } = true;
        public string? Description { get; set; }
    }

    public class HotelVenueDetailsDto
    {
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

        public bool HasCeremony { get; set; }
        public string? CeremonyLocation { get; set; }
        public string? OutdoorCeremonyCapacity { get; set; }
        public bool? SeparateCeremonyReceptionSpaces { get; set; }

        public bool HasCatering { get; set; }
        public string? CateringProvidedBy { get; set; }
        public string? OutsideFoodAllowed { get; set; }
        public string? KitchenFacility { get; set; }
        public string[]? CuisineOptions { get; set; }
        public bool? BuffetAvailable { get; set; }
        public bool? PlatedDinnerAvailable { get; set; }
        public bool? CustomMenuAvailable { get; set; }
        public bool? CakeCuttingAllowed { get; set; }

        public bool HasBeverages { get; set; }
        public string? BeverageService { get; set; }
        public string? BarFacility { get; set; }
        public string? OutsideBeveragesAllowed { get; set; }

        public bool HasAccommodation { get; set; }
        public string? NumberOfGuestRooms { get; set; }
        public string? ComplimentaryBridalSuite { get; set; }
        public string[]? RoomTypes { get; set; }
        public bool? BridalSuiteAvailable { get; set; }
        public bool? GuestAccommodationAvailable { get; set; }
        public bool? OnSiteAccommodation { get; set; }

        public bool HasEntertainment { get; set; }
        public string? DjAllowed { get; set; }
        public string? LiveBandAllowed { get; set; }
        public string? MaxMusicEndTime { get; set; }
        public string? ProjectorScreen { get; set; }
        public bool? TraditionalMusicAllowed { get; set; }

        public bool HasDecoration { get; set; }
        public string? DecorationPolicy { get; set; }
        public string? TableDecoration { get; set; }
        public string? LightingDecoration { get; set; }
        public string? OutsideDecoratorAllowed { get; set; }
        public bool? BasicDecorationIncluded { get; set; }
        public bool? FloralDecorationAvailable { get; set; }
        public bool? StageDecorationAvailable { get; set; }

        public bool HasPhotographyPolicy { get; set; }
        public bool? PhotographyAllowed { get; set; }
        public string? ExternalPhotographerAllowed { get; set; }
        public string? PreWeddingShootAllowed { get; set; }
        public string[]? PhotographyLocations { get; set; }

        public bool HasPolicies { get; set; }
        public bool? DepositRequired { get; set; }
        public string? DepositAmount { get; set; }
        public int? MinimumGuestCount { get; set; }
        public string? MinimumBookingDuration { get; set; }
        public string? CancellationPolicy { get; set; }
        public string? OutsideVendorRestrictions { get; set; }
        public string? AdditionalCharges { get; set; }
    }

    public class PhotographyDetailsDto
    {
        public string? ShootingStyle { get; set; }
        public string? HoursOfCoverage { get; set; }
        public string[]? IncludedServices { get; set; }
        public string? PhotosDelivered { get; set; }
        public string? DeliveryTimeframe { get; set; }
        public bool? RawFilesIncluded { get; set; }
        public bool? DigitalGalleryIncluded { get; set; }

        public bool AlbumIncluded { get; set; }
        public string? AlbumType { get; set; }
        public string? AlbumPages { get; set; }

        public string? PhotographerCount { get; set; }
        public bool? DroneAllowed { get; set; }
        public bool? BackupGear { get; set; }

        public bool VideographyIncluded { get; set; }
        public string? VideographerCount { get; set; }
        public string? VideoLength { get; set; }
        public string[]? VideoDeliverables { get; set; }

        public string? TravelOutsideColombo { get; set; }
        public bool? OutstationAccommodationRequired { get; set; }

        public bool DepositRequired { get; set; }
        public string? DepositAmount { get; set; }
        public string? CancellationPolicy { get; set; }
    }

    public class MusicDetailsDto
    {
        public string? PerformanceType { get; set; }
        public string? LineupSize { get; set; }
        public string? SetDuration { get; set; }
        public string[]? Genres { get; set; }

        public bool SoundSystemIncluded { get; set; }
        public string? SoundSystemCapacity { get; set; }
        public string? WirelessMics { get; set; }

        public bool StageLightingIncluded { get; set; }
        public string? LightingRig { get; set; }

        public string? SetupTimeRequired { get; set; }
        public bool? BackupHardwareOnSite { get; set; }
        public bool? McServicesIncluded { get; set; }
        public bool? BreakMusicIncluded { get; set; }
        public string? CustomSongsAllowed { get; set; }
        public string? OvertimeRate { get; set; }
    }

    public class DecorationsDetailsDto
    {
        public string[]? PrimaryStyles { get; set; }

        public bool ProvidesFlorals { get; set; }
        public string[]? FloralTypes { get; set; }

        public string[]? AvailableSetups { get; set; }

        public string? TablewareLinens { get; set; }
        public bool? CustomSignageIncluded { get; set; }
        public bool? LoungePropsAvailable { get; set; }

        public string? SetupTimeRequired { get; set; }
        public bool? SameDayTeardownIncluded { get; set; }
        public string? VenueRestrictions { get; set; }

        public bool OutstationDecorAllowed { get; set; }
        public string? TravelFeePolicy { get; set; }

        public bool? FreeConsultation { get; set; }
        public bool? CustomMoodboards { get; set; }
        public string? DesignFeePolicy { get; set; }
        public string? MinimumBudget { get; set; }
    }

    public class CateringDetailsDto
    {
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

        public bool TastingAvailable { get; set; }
        public string? TastingPolicy { get; set; }
    }

    public class VendorPerformanceRequestDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(2000)]
        public string? CustomerFeedback { get; set; }

        public DateTime? EventDate { get; set; }
    }

    public class VendorPerformanceResponseDto : VendorPerformanceRequestDto
    {
        public int PerformanceId { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
