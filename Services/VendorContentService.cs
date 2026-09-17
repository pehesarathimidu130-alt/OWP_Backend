using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Constants;
using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class VendorContentService : IVendorContentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public VendorContentService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<VendorServiceResponseDto>> GetServicesAsync(int userId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var services = await _context.VendorServices
                .Include(s => s.Category)
                .Include(s => s.Images)
                .Include(s => s.VenueSpaces)
                .Include(s => s.HotelVenueDetails)
                .Include(s => s.PhotographyDetails)
                .Include(s => s.MusicDetails)
                .Include(s => s.DecorationsDetails)
                .Include(s => s.CateringDetails)
                .Where(s => s.VendorId == vendorId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();

            return services.Select(MapToResponseDto).ToList();
        }

        public async Task<VendorServiceResponseDto> GetServiceByIdAsync(int userId, int serviceId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices
                .Include(s => s.Category)
                .Include(s => s.Images)
                .Include(s => s.VenueSpaces)
                .Include(s => s.HotelVenueDetails)
                .Include(s => s.PhotographyDetails)
                .Include(s => s.MusicDetails)
                .Include(s => s.DecorationsDetails)
                .Include(s => s.CateringDetails)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.VendorId == vendorId)
                ?? throw new KeyNotFoundException($"Service with ID {serviceId} was not found.");

            return MapToResponseDto(service);
        }

        public async Task<VendorServiceResponseDto> AddServiceAsync(int userId, VendorServiceRequestDto request)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var category = await ResolveCategoryAsync(request.CategoryId, request.Category);

            var service = new VendorService
            {
                VendorId = vendorId,
                CategoryId = category.CategoryId,
                ServiceName = request.Title.Trim(),
                ShortDescription = request.Description?.Trim() ?? string.Empty,
                Description = request.FullDescription?.Trim(),
                Price = request.PriceOnRequest ? null : request.Price,
                IsPriceOnRequest = request.PriceOnRequest,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim(),
                CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VendorServices.Add(service);
            await _context.SaveChangesAsync();

            // Save category detail and venue spaces for the newly created service
            await SaveCategoryDetailsAsync(service, category.CategoryId, request, isNew: true);

            bool isPublished = string.Equals(service.Status, "Published", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(service.Status, "Active", StringComparison.OrdinalIgnoreCase);

            if (isPublished)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Listing Published",
                    Message = $"Your listing \"{service.ServiceName}\" has been published successfully.",
                    Type = NotificationTypes.ListingPublished,
                    IsRead = false
                });
            }
            else
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Listing Created",
                    Message = $"Your listing \"{service.ServiceName}\" was created as a draft.",
                    Type = NotificationTypes.ListingCreated,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            return await GetServiceByIdAsync(userId, service.ServiceId);
        }

        public async Task<VendorServiceResponseDto> UpdateServiceAsync(int userId, int serviceId, VendorServiceRequestDto request)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices
                .Include(s => s.Category)
                .Include(s => s.Images)
                .Include(s => s.VenueSpaces)
                .Include(s => s.HotelVenueDetails)
                .Include(s => s.PhotographyDetails)
                .Include(s => s.MusicDetails)
                .Include(s => s.DecorationsDetails)
                .Include(s => s.CateringDetails)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.VendorId == vendorId)
                ?? throw new KeyNotFoundException($"Service with ID {serviceId} was not found.");

            var category = await ResolveCategoryAsync(request.CategoryId, request.Category);

            var previousStatus = service.Status;
            var newStatus = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim();

            service.ServiceName = request.Title.Trim();
            service.ShortDescription = request.Description?.Trim() ?? string.Empty;
            service.Description = request.FullDescription?.Trim();
            service.Price = request.PriceOnRequest ? null : request.Price;
            service.IsPriceOnRequest = request.PriceOnRequest;
            service.Status = newStatus;
            service.CategoryId = category.CategoryId;
            if (request.CoverImageUrl != null)
            {
                service.CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim();
            }
            service.UpdatedAt = DateTime.UtcNow;

            // Enforce single-detail-table rule and update active category details
            await SaveCategoryDetailsAsync(service, category.CategoryId, request, isNew: false);

            bool wasPublished = string.Equals(previousStatus, "Published", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(previousStatus, "Active", StringComparison.OrdinalIgnoreCase);
            bool isNowPublished = string.Equals(service.Status, "Published", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(service.Status, "Active", StringComparison.OrdinalIgnoreCase);

            if (!wasPublished && isNowPublished)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Listing Published",
                    Message = $"Your listing \"{service.ServiceName}\" has been published successfully.",
                    Type = NotificationTypes.ListingPublished,
                    IsRead = false
                });
            }
            else
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Listing Updated",
                    Message = $"Your listing \"{service.ServiceName}\" has been updated successfully.",
                    Type = NotificationTypes.ListingCreated,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            return await GetServiceByIdAsync(userId, service.ServiceId);
        }

        public async Task DeleteServiceAsync(int userId, int serviceId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.VendorId == vendorId)
                ?? throw new KeyNotFoundException($"Service with ID {serviceId} was not found.");

            foreach (var img in service.Images)
            {
                DeleteFile(img.ImageUrl);
            }

            _context.VendorServices.Remove(service);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // Category Detail & VenueSpaces Sync
        // ─────────────────────────────────────────────────────────────

        private async Task SaveCategoryDetailsAsync(VendorService service, int categoryId, VendorServiceRequestDto request, bool isNew)
        {
            // Purge other 4 category details so EXACTLY ONE detail table is populated
            if (categoryId != 1)
            {
                if (service.HotelVenueDetails != null)
                {
                    _context.HotelVenueDetails.Remove(service.HotelVenueDetails);
                    service.HotelVenueDetails = null;
                }
                if (service.VenueSpaces.Count > 0)
                {
                    _context.VenueSpaces.RemoveRange(service.VenueSpaces);
                    service.VenueSpaces.Clear();
                }
            }
            if (categoryId != 2 && service.PhotographyDetails != null)
            {
                _context.PhotographyDetails.Remove(service.PhotographyDetails);
                service.PhotographyDetails = null;
            }
            if (categoryId != 3 && service.MusicDetails != null)
            {
                _context.MusicDetails.Remove(service.MusicDetails);
                service.MusicDetails = null;
            }
            if (categoryId != 4 && service.DecorationsDetails != null)
            {
                _context.DecorationsDetails.Remove(service.DecorationsDetails);
                service.DecorationsDetails = null;
            }
            if (categoryId != 5 && service.CateringDetails != null)
            {
                _context.CateringDetails.Remove(service.CateringDetails);
                service.CateringDetails = null;
            }

            // Populate active category details
            switch (categoryId)
            {
                case 1: // Hotel / Venue
                    await SyncHotelVenueAsync(service, request);
                    break;
                case 2: // Photography
                    SyncPhotography(service, request);
                    break;
                case 3: // Music
                    SyncMusic(service, request);
                    break;
                case 4: // Decorations
                    SyncDecorations(service, request);
                    break;
                case 5: // Catering
                    SyncCatering(service, request);
                    break;
                default:
                    throw new ArgumentException($"Unsupported category ID: {categoryId}");
            }
        }

        private async Task SyncHotelVenueAsync(VendorService service, VendorServiceRequestDto request)
        {
            var dto = request.HotelVenueDetails;
            if (dto == null && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                dto = JsonSerializer.Deserialize<HotelVenueDetailsDto>(request.Details.Value.GetRawText(), JsonOptions);
            }

            dto ??= new HotelVenueDetailsDto();

            var entity = service.HotelVenueDetails;
            if (entity == null)
            {
                entity = new HotelVenueDetails { ServiceId = service.ServiceId };
                _context.HotelVenueDetails.Add(entity);
                service.HotelVenueDetails = entity;
            }

            // Map all HotelVenue fields
            entity.VenueType = dto.VenueType;
            entity.VenueSetting = dto.VenueSetting;
            entity.IndoorOutdoor = dto.IndoorOutdoor;
            entity.ParkingCapacity = dto.ParkingCapacity;
            entity.ParkingType = dto.ParkingType;
            entity.ValetParking = dto.ValetParking;
            entity.Wifi = dto.Wifi;
            entity.WheelchairAccessible = dto.WheelchairAccessible;
            entity.HasAirConditioning = dto.HasAirConditioning;
            entity.HasBackupGenerator = dto.HasBackupGenerator;
            entity.HasElevator = dto.HasElevator;
            entity.HasGuestDropOff = dto.HasGuestDropOff;
            entity.HasVendorLoadingAccess = dto.HasVendorLoadingAccess;

            entity.HasCeremony = dto.HasCeremony;
            entity.CeremonyLocation = dto.CeremonyLocation;
            entity.OutdoorCeremonyCapacity = dto.OutdoorCeremonyCapacity;
            entity.SeparateCeremonyReceptionSpaces = dto.SeparateCeremonyReceptionSpaces;

            entity.HasCatering = dto.HasCatering;
            entity.CateringProvidedBy = dto.CateringProvidedBy;
            entity.OutsideFoodAllowed = dto.OutsideFoodAllowed;
            entity.KitchenFacility = dto.KitchenFacility;
            entity.CuisineOptions = dto.CuisineOptions;
            entity.BuffetAvailable = dto.BuffetAvailable;
            entity.PlatedDinnerAvailable = dto.PlatedDinnerAvailable;
            entity.CustomMenuAvailable = dto.CustomMenuAvailable;
            entity.CakeCuttingAllowed = dto.CakeCuttingAllowed;

            entity.HasBeverages = dto.HasBeverages;
            entity.BeverageService = dto.BeverageService;
            entity.BarFacility = dto.BarFacility;
            entity.OutsideBeveragesAllowed = dto.OutsideBeveragesAllowed;

            entity.HasAccommodation = dto.HasAccommodation;
            entity.NumberOfGuestRooms = dto.NumberOfGuestRooms;
            entity.ComplimentaryBridalSuite = dto.ComplimentaryBridalSuite;
            entity.RoomTypes = dto.RoomTypes;
            entity.BridalSuiteAvailable = dto.BridalSuiteAvailable;
            entity.GuestAccommodationAvailable = dto.GuestAccommodationAvailable;
            entity.OnSiteAccommodation = dto.OnSiteAccommodation;

            entity.HasEntertainment = dto.HasEntertainment;
            entity.DjAllowed = dto.DjAllowed;
            entity.LiveBandAllowed = dto.LiveBandAllowed;
            entity.MaxMusicEndTime = dto.MaxMusicEndTime;
            entity.ProjectorScreen = dto.ProjectorScreen;
            entity.TraditionalMusicAllowed = dto.TraditionalMusicAllowed;

            entity.HasDecoration = dto.HasDecoration;
            entity.DecorationPolicy = dto.DecorationPolicy;
            entity.TableDecoration = dto.TableDecoration;
            entity.LightingDecoration = dto.LightingDecoration;
            entity.OutsideDecoratorAllowed = dto.OutsideDecoratorAllowed;
            entity.BasicDecorationIncluded = dto.BasicDecorationIncluded;
            entity.FloralDecorationAvailable = dto.FloralDecorationAvailable;
            entity.StageDecorationAvailable = dto.StageDecorationAvailable;

            entity.HasPhotographyPolicy = dto.HasPhotographyPolicy;
            entity.PhotographyAllowed = dto.PhotographyAllowed;
            entity.ExternalPhotographerAllowed = dto.ExternalPhotographerAllowed;
            entity.PreWeddingShootAllowed = dto.PreWeddingShootAllowed;
            entity.PhotographyLocations = dto.PhotographyLocations;

            entity.HasPolicies = dto.HasPolicies;
            entity.DepositRequired = dto.DepositRequired;
            entity.DepositAmount = dto.DepositAmount;
            entity.MinimumGuestCount = dto.MinimumGuestCount;
            entity.MinimumBookingDuration = dto.MinimumBookingDuration;
            entity.CancellationPolicy = dto.CancellationPolicy;
            entity.OutsideVendorRestrictions = dto.OutsideVendorRestrictions;
            entity.AdditionalCharges = dto.AdditionalCharges;

            // Sync VenueSpaces
            var spacesList = request.Spaces;
            if ((spacesList == null || spacesList.Count == 0) && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                if (request.Details.Value.TryGetProperty("spaces", out var spacesEl) && spacesEl.ValueKind == JsonValueKind.Array)
                {
                    spacesList = JsonSerializer.Deserialize<List<VenueSpaceDto>>(spacesEl.GetRawText(), JsonOptions);
                }
            }

            spacesList ??= new List<VenueSpaceDto>();

            // Remove existing spaces not in incoming list
            var incomingIds = spacesList
                .Select(s => s.VenueSpaceId ?? s.SpaceId)
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .ToHashSet();

            var toRemove = service.VenueSpaces.Where(s => !incomingIds.Contains(s.VenueSpaceId)).ToList();
            if (toRemove.Count > 0)
            {
                _context.VenueSpaces.RemoveRange(toRemove);
            }

            foreach (var sp in spacesList)
            {
                var targetId = sp.VenueSpaceId ?? sp.SpaceId;
                if (targetId.HasValue && targetId.Value > 0)
                {
                    var existing = service.VenueSpaces.FirstOrDefault(s => s.VenueSpaceId == targetId.Value);
                    if (existing != null)
                    {
                        existing.SpaceName = sp.Name.Trim();
                        existing.SpaceType = sp.Type ?? "Indoor Ballroom";
                        existing.SeatedCapacity = sp.CapacitySeated;
                        existing.FloatingCapacity = sp.CapacityFloating;
                        existing.AirConditioned = sp.IsAirConditioned;
                        existing.KeyFeatures = sp.Description;
                        continue;
                    }
                }

                // Add new space
                _context.VenueSpaces.Add(new VenueSpace
                {
                    ServiceId = service.ServiceId,
                    SpaceName = sp.Name.Trim(),
                    SpaceType = sp.Type ?? "Indoor Ballroom",
                    SeatedCapacity = sp.CapacitySeated,
                    FloatingCapacity = sp.CapacityFloating,
                    AirConditioned = sp.IsAirConditioned,
                    KeyFeatures = sp.Description
                });
            }
        }

        private void SyncPhotography(VendorService service, VendorServiceRequestDto request)
        {
            var dto = request.PhotographyDetails;
            if (dto == null && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                dto = JsonSerializer.Deserialize<PhotographyDetailsDto>(request.Details.Value.GetRawText(), JsonOptions);
            }

            dto ??= new PhotographyDetailsDto();

            var entity = service.PhotographyDetails;
            if (entity == null)
            {
                entity = new PhotographyDetails { ServiceId = service.ServiceId };
                _context.PhotographyDetails.Add(entity);
                service.PhotographyDetails = entity;
            }

            entity.ShootingStyle = dto.ShootingStyle;
            entity.HoursOfCoverage = dto.HoursOfCoverage;
            entity.IncludedServices = dto.IncludedServices;
            entity.PhotosDelivered = dto.PhotosDelivered;
            entity.DeliveryTimeframe = dto.DeliveryTimeframe;
            entity.RawFilesIncluded = dto.RawFilesIncluded;
            entity.DigitalGalleryIncluded = dto.DigitalGalleryIncluded;

            entity.AlbumIncluded = dto.AlbumIncluded;
            entity.AlbumType = dto.AlbumType;
            entity.AlbumPages = dto.AlbumPages;

            entity.PhotographerCount = dto.PhotographerCount;
            entity.DroneAllowed = dto.DroneAllowed;
            entity.BackupGear = dto.BackupGear;

            entity.VideographyIncluded = dto.VideographyIncluded;
            entity.VideographerCount = dto.VideographerCount;
            entity.VideoLength = dto.VideoLength;
            entity.VideoDeliverables = dto.VideoDeliverables;

            entity.TravelOutsideColombo = dto.TravelOutsideColombo;
            entity.OutstationAccommodationRequired = dto.OutstationAccommodationRequired;

            entity.DepositRequired = dto.DepositRequired;
            entity.DepositAmount = dto.DepositAmount;
            entity.CancellationPolicy = dto.CancellationPolicy;
        }

        private void SyncMusic(VendorService service, VendorServiceRequestDto request)
        {
            var dto = request.MusicDetails;
            if (dto == null && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                dto = JsonSerializer.Deserialize<MusicDetailsDto>(request.Details.Value.GetRawText(), JsonOptions);
            }

            dto ??= new MusicDetailsDto();

            var entity = service.MusicDetails;
            if (entity == null)
            {
                entity = new MusicDetails { ServiceId = service.ServiceId };
                _context.MusicDetails.Add(entity);
                service.MusicDetails = entity;
            }

            entity.PerformanceType = dto.PerformanceType;
            entity.LineupSize = dto.LineupSize;
            entity.SetDuration = dto.SetDuration;
            entity.Genres = dto.Genres;

            entity.SoundSystemIncluded = dto.SoundSystemIncluded;
            entity.SoundSystemCapacity = dto.SoundSystemCapacity;
            entity.WirelessMics = dto.WirelessMics;

            entity.StageLightingIncluded = dto.StageLightingIncluded;
            entity.LightingRig = dto.LightingRig;

            entity.SetupTimeRequired = dto.SetupTimeRequired;
            entity.BackupHardwareOnSite = dto.BackupHardwareOnSite;
            entity.McServicesIncluded = dto.McServicesIncluded;
            entity.BreakMusicIncluded = dto.BreakMusicIncluded;
            entity.CustomSongsAllowed = dto.CustomSongsAllowed;
            entity.OvertimeRate = dto.OvertimeRate;
        }

        private void SyncDecorations(VendorService service, VendorServiceRequestDto request)
        {
            var dto = request.DecorationsDetails;
            if (dto == null && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                dto = JsonSerializer.Deserialize<DecorationsDetailsDto>(request.Details.Value.GetRawText(), JsonOptions);
            }

            dto ??= new DecorationsDetailsDto();

            var entity = service.DecorationsDetails;
            if (entity == null)
            {
                entity = new DecorationsDetails { ServiceId = service.ServiceId };
                _context.DecorationsDetails.Add(entity);
                service.DecorationsDetails = entity;
            }

            entity.PrimaryStyles = dto.PrimaryStyles;
            entity.ProvidesFlorals = dto.ProvidesFlorals;
            entity.FloralTypes = dto.FloralTypes;
            entity.AvailableSetups = dto.AvailableSetups;
            entity.TablewareLinens = dto.TablewareLinens;
            entity.CustomSignageIncluded = dto.CustomSignageIncluded;
            entity.LoungePropsAvailable = dto.LoungePropsAvailable;

            entity.SetupTimeRequired = dto.SetupTimeRequired;
            entity.SameDayTeardownIncluded = dto.SameDayTeardownIncluded;
            entity.VenueRestrictions = dto.VenueRestrictions;

            entity.OutstationDecorAllowed = dto.OutstationDecorAllowed;
            entity.TravelFeePolicy = dto.TravelFeePolicy;

            entity.FreeConsultation = dto.FreeConsultation;
            entity.CustomMoodboards = dto.CustomMoodboards;
            entity.DesignFeePolicy = dto.DesignFeePolicy;
            entity.MinimumBudget = dto.MinimumBudget;
        }

        private void SyncCatering(VendorService service, VendorServiceRequestDto request)
        {
            var dto = request.CateringDetails;
            if (dto == null && request.Details.HasValue && request.Details.Value.ValueKind == JsonValueKind.Object)
            {
                dto = JsonSerializer.Deserialize<CateringDetailsDto>(request.Details.Value.GetRawText(), JsonOptions);
            }

            dto ??= new CateringDetailsDto();

            var entity = service.CateringDetails;
            if (entity == null)
            {
                entity = new CateringDetails { ServiceId = service.ServiceId };
                _context.CateringDetails.Add(entity);
                service.CateringDetails = entity;
            }

            entity.ServiceStyle = dto.ServiceStyle;
            entity.Cuisines = dto.Cuisines;
            entity.DietaryOptions = dto.DietaryOptions;
            entity.MinGuests = dto.MinGuests;
            entity.MaxGuests = dto.MaxGuests;
            entity.PricePerHead = dto.PricePerHead;

            entity.WaitstaffIncluded = dto.WaitstaffIncluded;
            entity.GlasswareIncluded = dto.GlasswareIncluded;
            entity.CrockeryCutlery = dto.CrockeryCutlery;
            entity.ChafingDishesIncluded = dto.ChafingDishesIncluded;
            entity.FurnitureRentalAvailable = dto.FurnitureRentalAvailable;
            entity.SetupTeardownIncluded = dto.SetupTeardownIncluded;

            entity.OutstationCatering = dto.OutstationCatering;
            entity.KitchenRequirement = dto.KitchenRequirement;

            entity.TastingAvailable = dto.TastingAvailable;
            entity.TastingPolicy = dto.TastingPolicy;
        }

        // ─────────────────────────────────────────────────────────────
        // Response Mapping
        // ─────────────────────────────────────────────────────────────

        private static VendorServiceResponseDto MapToResponseDto(VendorService service)
        {
            var response = new VendorServiceResponseDto
            {
                ServiceId = service.ServiceId,
                VendorId = service.VendorId,
                CategoryId = service.CategoryId,
                Category = service.Category?.CategoryName ?? string.Empty,
                Title = service.ServiceName,
                Description = service.ShortDescription,
                FullDescription = service.Description,
                Price = service.Price,
                PriceOnRequest = service.IsPriceOnRequest,
                Status = service.Status,
                CoverImageUrl = service.CoverImageUrl,
                Images = service.Images != null && service.Images.Count > 0
                    ? service.Images
                        .OrderBy(img => img.DisplayOrder)
                        .ThenBy(img => img.ImageId)
                        .Select(img => new VendorServiceImageDto
                        {
                            ImageId = img.ImageId,
                            ServiceId = img.ServiceId,
                            ImageUrl = img.ImageUrl,
                            IsCover = img.IsCover,
                            DisplayOrder = img.DisplayOrder,
                            CreatedAt = img.CreatedAt
                        })
                        .ToList()
                    : new List<VendorServiceImageDto>(),
                Views = 0,
                Inquiries = 0,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            };

            // Unified details dictionary populated for frontend wizard consumption
            var detailsDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (service.CategoryId == 1) // Hotel / Venue
            {
                var spacesDto = service.VenueSpaces.Select(s => new VenueSpaceDto
                {
                    SpaceId = s.VenueSpaceId,
                    Name = s.SpaceName,
                    Type = s.SpaceType,
                    CapacitySeated = s.SeatedCapacity,
                    CapacityFloating = s.FloatingCapacity,
                    IsAirConditioned = s.AirConditioned ?? true,
                    Description = s.KeyFeatures
                }).ToList();

                response.Spaces = spacesDto;
                detailsDict["spaces"] = spacesDto;

                if (service.HotelVenueDetails != null)
                {
                    var h = service.HotelVenueDetails;
                    response.HotelVenueDetails = new HotelVenueDetailsDto
                    {
                        VenueType = h.VenueType,
                        VenueSetting = h.VenueSetting,
                        IndoorOutdoor = h.IndoorOutdoor,
                        ParkingCapacity = h.ParkingCapacity,
                        ParkingType = h.ParkingType,
                        ValetParking = h.ValetParking,
                        Wifi = h.Wifi,
                        WheelchairAccessible = h.WheelchairAccessible,
                        HasAirConditioning = h.HasAirConditioning,
                        HasBackupGenerator = h.HasBackupGenerator,
                        HasElevator = h.HasElevator,
                        HasGuestDropOff = h.HasGuestDropOff,
                        HasVendorLoadingAccess = h.HasVendorLoadingAccess,
                        HasCeremony = h.HasCeremony,
                        CeremonyLocation = h.CeremonyLocation,
                        OutdoorCeremonyCapacity = h.OutdoorCeremonyCapacity,
                        SeparateCeremonyReceptionSpaces = h.SeparateCeremonyReceptionSpaces,
                        HasCatering = h.HasCatering,
                        CateringProvidedBy = h.CateringProvidedBy,
                        OutsideFoodAllowed = h.OutsideFoodAllowed,
                        KitchenFacility = h.KitchenFacility,
                        CuisineOptions = h.CuisineOptions,
                        BuffetAvailable = h.BuffetAvailable,
                        PlatedDinnerAvailable = h.PlatedDinnerAvailable,
                        CustomMenuAvailable = h.CustomMenuAvailable,
                        CakeCuttingAllowed = h.CakeCuttingAllowed,
                        HasBeverages = h.HasBeverages,
                        BeverageService = h.BeverageService,
                        BarFacility = h.BarFacility,
                        OutsideBeveragesAllowed = h.OutsideBeveragesAllowed,
                        HasAccommodation = h.HasAccommodation,
                        NumberOfGuestRooms = h.NumberOfGuestRooms,
                        ComplimentaryBridalSuite = h.ComplimentaryBridalSuite,
                        RoomTypes = h.RoomTypes,
                        BridalSuiteAvailable = h.BridalSuiteAvailable,
                        GuestAccommodationAvailable = h.GuestAccommodationAvailable,
                        OnSiteAccommodation = h.OnSiteAccommodation,
                        HasEntertainment = h.HasEntertainment,
                        DjAllowed = h.DjAllowed,
                        LiveBandAllowed = h.LiveBandAllowed,
                        MaxMusicEndTime = h.MaxMusicEndTime,
                        ProjectorScreen = h.ProjectorScreen,
                        TraditionalMusicAllowed = h.TraditionalMusicAllowed,
                        HasDecoration = h.HasDecoration,
                        DecorationPolicy = h.DecorationPolicy,
                        TableDecoration = h.TableDecoration,
                        LightingDecoration = h.LightingDecoration,
                        OutsideDecoratorAllowed = h.OutsideDecoratorAllowed,
                        BasicDecorationIncluded = h.BasicDecorationIncluded,
                        FloralDecorationAvailable = h.FloralDecorationAvailable,
                        StageDecorationAvailable = h.StageDecorationAvailable,
                        HasPhotographyPolicy = h.HasPhotographyPolicy,
                        PhotographyAllowed = h.PhotographyAllowed,
                        ExternalPhotographerAllowed = h.ExternalPhotographerAllowed,
                        PreWeddingShootAllowed = h.PreWeddingShootAllowed,
                        PhotographyLocations = h.PhotographyLocations,
                        HasPolicies = h.HasPolicies,
                        DepositRequired = h.DepositRequired,
                        DepositAmount = h.DepositAmount,
                        MinimumGuestCount = h.MinimumGuestCount,
                        MinimumBookingDuration = h.MinimumBookingDuration,
                        CancellationPolicy = h.CancellationPolicy,
                        OutsideVendorRestrictions = h.OutsideVendorRestrictions,
                        AdditionalCharges = h.AdditionalCharges
                    };

                    // Populate flat details dict
                    foreach (var prop in typeof(HotelVenueDetailsDto).GetProperties())
                    {
                        detailsDict[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = prop.GetValue(response.HotelVenueDetails);
                    }
                }
            }
            else if (service.CategoryId == 2 && service.PhotographyDetails != null)
            {
                var p = service.PhotographyDetails;
                response.PhotographyDetails = new PhotographyDetailsDto
                {
                    ShootingStyle = p.ShootingStyle,
                    HoursOfCoverage = p.HoursOfCoverage,
                    IncludedServices = p.IncludedServices,
                    PhotosDelivered = p.PhotosDelivered,
                    DeliveryTimeframe = p.DeliveryTimeframe,
                    RawFilesIncluded = p.RawFilesIncluded,
                    DigitalGalleryIncluded = p.DigitalGalleryIncluded,
                    AlbumIncluded = p.AlbumIncluded,
                    AlbumType = p.AlbumType,
                    AlbumPages = p.AlbumPages,
                    PhotographerCount = p.PhotographerCount,
                    DroneAllowed = p.DroneAllowed,
                    BackupGear = p.BackupGear,
                    VideographyIncluded = p.VideographyIncluded,
                    VideographerCount = p.VideographerCount,
                    VideoLength = p.VideoLength,
                    VideoDeliverables = p.VideoDeliverables,
                    TravelOutsideColombo = p.TravelOutsideColombo,
                    OutstationAccommodationRequired = p.OutstationAccommodationRequired,
                    DepositRequired = p.DepositRequired,
                    DepositAmount = p.DepositAmount,
                    CancellationPolicy = p.CancellationPolicy
                };

                foreach (var prop in typeof(PhotographyDetailsDto).GetProperties())
                {
                    detailsDict[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = prop.GetValue(response.PhotographyDetails);
                }
            }
            else if (service.CategoryId == 3 && service.MusicDetails != null)
            {
                var m = service.MusicDetails;
                response.MusicDetails = new MusicDetailsDto
                {
                    PerformanceType = m.PerformanceType,
                    LineupSize = m.LineupSize,
                    SetDuration = m.SetDuration,
                    Genres = m.Genres,
                    SoundSystemIncluded = m.SoundSystemIncluded,
                    SoundSystemCapacity = m.SoundSystemCapacity,
                    WirelessMics = m.WirelessMics,
                    StageLightingIncluded = m.StageLightingIncluded,
                    LightingRig = m.LightingRig,
                    SetupTimeRequired = m.SetupTimeRequired,
                    BackupHardwareOnSite = m.BackupHardwareOnSite,
                    McServicesIncluded = m.McServicesIncluded,
                    BreakMusicIncluded = m.BreakMusicIncluded,
                    CustomSongsAllowed = m.CustomSongsAllowed,
                    OvertimeRate = m.OvertimeRate
                };

                foreach (var prop in typeof(MusicDetailsDto).GetProperties())
                {
                    detailsDict[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = prop.GetValue(response.MusicDetails);
                }
            }
            else if (service.CategoryId == 4 && service.DecorationsDetails != null)
            {
                var d = service.DecorationsDetails;
                response.DecorationsDetails = new DecorationsDetailsDto
                {
                    PrimaryStyles = d.PrimaryStyles,
                    ProvidesFlorals = d.ProvidesFlorals,
                    FloralTypes = d.FloralTypes,
                    AvailableSetups = d.AvailableSetups,
                    TablewareLinens = d.TablewareLinens,
                    CustomSignageIncluded = d.CustomSignageIncluded,
                    LoungePropsAvailable = d.LoungePropsAvailable,
                    SetupTimeRequired = d.SetupTimeRequired,
                    SameDayTeardownIncluded = d.SameDayTeardownIncluded,
                    VenueRestrictions = d.VenueRestrictions,
                    OutstationDecorAllowed = d.OutstationDecorAllowed,
                    TravelFeePolicy = d.TravelFeePolicy,
                    FreeConsultation = d.FreeConsultation,
                    CustomMoodboards = d.CustomMoodboards,
                    DesignFeePolicy = d.DesignFeePolicy,
                    MinimumBudget = d.MinimumBudget
                };

                foreach (var prop in typeof(DecorationsDetailsDto).GetProperties())
                {
                    detailsDict[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = prop.GetValue(response.DecorationsDetails);
                }
            }
            else if (service.CategoryId == 5 && service.CateringDetails != null)
            {
                var c = service.CateringDetails;
                response.CateringDetails = new CateringDetailsDto
                {
                    ServiceStyle = c.ServiceStyle,
                    Cuisines = c.Cuisines,
                    DietaryOptions = c.DietaryOptions,
                    MinGuests = c.MinGuests,
                    MaxGuests = c.MaxGuests,
                    PricePerHead = c.PricePerHead,
                    WaitstaffIncluded = c.WaitstaffIncluded,
                    GlasswareIncluded = c.GlasswareIncluded,
                    CrockeryCutlery = c.CrockeryCutlery,
                    ChafingDishesIncluded = c.ChafingDishesIncluded,
                    FurnitureRentalAvailable = c.FurnitureRentalAvailable,
                    SetupTeardownIncluded = c.SetupTeardownIncluded,
                    OutstationCatering = c.OutstationCatering,
                    KitchenRequirement = c.KitchenRequirement,
                    TastingAvailable = c.TastingAvailable,
                    TastingPolicy = c.TastingPolicy
                };

                foreach (var prop in typeof(CateringDetailsDto).GetProperties())
                {
                    detailsDict[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = prop.GetValue(response.CateringDetails);
                }
            }

            response.Details = detailsDict;
            return response;
        }

        private async Task<Category> ResolveCategoryAsync(int? categoryId, string? categoryName)
        {
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId.Value);
                if (category != null) return category;
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var trimmed = categoryName.Trim();
                var normalized = trimmed.Replace(" ", "").Replace("/", "").ToLowerInvariant();

                var allCategories = await _context.Categories.ToListAsync();
                var matched = allCategories.FirstOrDefault(c =>
                    c.CategoryName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    c.CategoryName.Replace(" ", "").Replace("/", "").ToLowerInvariant() == normalized);

                if (matched != null) return matched;
            }

            throw new ArgumentException($"Invalid or unspecified category '{categoryName ?? categoryId?.ToString()}'. Valid categories are: Hotel / Venue, Photography, Music, Decorations, Catering.");
        }

        private async Task<int> GetVendorIdAsync(int userId)
        {
            var vendorId = await _context.Vendors
                .Where(vendor => vendor.UserId == userId)
                .Select(vendor => (int?)vendor.VendorId)
                .FirstOrDefaultAsync();

            return vendorId ?? throw new KeyNotFoundException("Vendor profile was not found for this user.");
        }

        // ─────────────────────────────────────────────────────────────
        // Performances & Notifications
        // ─────────────────────────────────────────────────────────────

        public async Task<List<VendorPerformanceResponseDto>> GetPerformancesAsync(int userId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            return await _context.VendorPerformances
                .Where(performance => performance.VendorId == vendorId)
                .OrderByDescending(performance => performance.EventDate ?? performance.CreatedAt)
                .Select(performance => new VendorPerformanceResponseDto
                {
                    PerformanceId = performance.PerformanceId,
                    Title = performance.Title,
                    Category = performance.Category,
                    Description = performance.Description,
                    PhotoUrl = performance.PhotoUrl,
                    CustomerName = performance.CustomerName,
                    CustomerFeedback = performance.CustomerFeedback,
                    EventDate = performance.EventDate
                })
                .ToListAsync();
        }

        public async Task<VendorPerformanceResponseDto> AddPerformanceAsync(int userId, VendorPerformanceRequestDto request, IFormFile? photo)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var performance = new VendorPerformance
            {
                VendorId = vendorId,
                Title = request.Title.Trim(),
                Category = request.Category.Trim(),
                Description = request.Description?.Trim(),
                PhotoUrl = await SavePhotoAsync(photo),
                CustomerName = request.CustomerName?.Trim(),
                CustomerFeedback = request.CustomerFeedback?.Trim(),
                EventDate = request.EventDate
            };

            _context.VendorPerformances.Add(performance);
            await _context.SaveChangesAsync();
            return MapPerformance(performance);
        }

        public async Task<VendorPerformanceResponseDto> UpdatePerformanceAsync(int userId, int performanceId, VendorPerformanceRequestDto request, IFormFile? photo)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var performance = await _context.VendorPerformances.FirstOrDefaultAsync(item => item.PerformanceId == performanceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Performance was not found.");

            performance.Title = request.Title.Trim();
            performance.Category = request.Category.Trim();
            performance.Description = request.Description?.Trim();
            performance.CustomerName = request.CustomerName?.Trim();
            performance.CustomerFeedback = request.CustomerFeedback?.Trim();
            performance.EventDate = request.EventDate;
            if (photo != null)
            {
                DeletePhoto(performance.PhotoUrl);
                performance.PhotoUrl = await SavePhotoAsync(photo);
            }
            await _context.SaveChangesAsync();
            return MapPerformance(performance);
        }

        public async Task DeletePerformanceAsync(int userId, int performanceId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var performance = await _context.VendorPerformances.FirstOrDefaultAsync(item => item.PerformanceId == performanceId && item.VendorId == vendorId)
                ?? throw new KeyNotFoundException("Performance was not found.");
            DeletePhoto(performance.PhotoUrl);
            _context.VendorPerformances.Remove(performance);
            await _context.SaveChangesAsync();
        }

        private static VendorPerformanceResponseDto MapPerformance(VendorPerformance performance) => new()
        {
            PerformanceId = performance.PerformanceId,
            Title = performance.Title,
            Category = performance.Category,
            Description = performance.Description,
            PhotoUrl = performance.PhotoUrl,
            CustomerName = performance.CustomerName,
            CustomerFeedback = performance.CustomerFeedback,
            EventDate = performance.EventDate
        };

        private async Task<string?> SavePhotoAsync(IFormFile? photo)
        {
            if (photo == null || photo.Length == 0) return null;
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only JPG, JPEG, PNG, and WEBP images are supported.");
            }
            if (photo.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Photos must be 5 MB or smaller.");
            }
            var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "vendor-performance");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = File.Create(Path.Combine(folder, fileName));
            await photo.CopyToAsync(stream);
            return $"/uploads/vendor-performance/{fileName}";
        }

        public async Task<VendorServiceImageDto> UploadServiceImageAsync(int userId, int serviceId, IFormFile file, bool isCover = false)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.VendorId == vendorId)
                ?? throw new KeyNotFoundException($"Service with ID {serviceId} was not found.");

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Please provide a valid image file.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Unsupported file format '{extension}'. Allowed: {string.Join(", ", allowedExtensions)}");
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                throw new ArgumentException("Image size must be 10 MB or smaller.");
            }

            var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(rootPath, "uploads", "vendor-services");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var destinationPath = Path.Combine(folder, fileName);

            await using (var stream = File.Create(destinationPath))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/vendor-services/{fileName}";

            var shouldBeCover = isCover || string.IsNullOrWhiteSpace(service.CoverImageUrl) || service.Images.Count == 0;

            if (shouldBeCover)
            {
                service.CoverImageUrl = relativeUrl;
                foreach (var existing in service.Images)
                {
                    existing.IsCover = false;
                }
            }

            var serviceImage = new VendorServiceImage
            {
                ServiceId = serviceId,
                ImageUrl = relativeUrl,
                IsCover = shouldBeCover,
                DisplayOrder = service.Images.Count,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VendorServiceImages.Add(serviceImage);
            service.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new VendorServiceImageDto
            {
                ImageId = serviceImage.ImageId,
                ServiceId = serviceImage.ServiceId,
                ImageUrl = serviceImage.ImageUrl,
                IsCover = serviceImage.IsCover,
                DisplayOrder = serviceImage.DisplayOrder,
                CreatedAt = serviceImage.CreatedAt
            };
        }

        public async Task DeleteServiceImageAsync(int userId, int serviceId, int imageId)
        {
            var vendorId = await GetVendorIdAsync(userId);
            var service = await _context.VendorServices
                .Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.VendorId == vendorId)
                ?? throw new KeyNotFoundException($"Service with ID {serviceId} was not found.");

            var image = service.Images.FirstOrDefault(i => i.ImageId == imageId)
                ?? throw new KeyNotFoundException($"Image with ID {imageId} was not found for this service.");

            DeleteFile(image.ImageUrl);
            _context.VendorServiceImages.Remove(image);

            if (image.IsCover || service.CoverImageUrl == image.ImageUrl)
            {
                var nextImage = service.Images.FirstOrDefault(i => i.ImageId != imageId);
                if (nextImage != null)
                {
                    nextImage.IsCover = true;
                    service.CoverImageUrl = nextImage.ImageUrl;
                }
                else
                {
                    service.CoverImageUrl = null;
                }
            }

            service.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private void DeleteFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var cleanPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(rootPath, cleanPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch { }
        }

        private void DeletePhoto(string? photoUrl)
        {
            DeleteFile(photoUrl);
        }
    }
}
