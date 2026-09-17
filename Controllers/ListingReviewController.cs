using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    /// <summary>
    /// ListingReviewController — Provides Admin and SuperAdmin endpoints to
    /// list, inspect, approve, reject, and toggle vendor service listings.
    /// Route prefix: /api/admin/listing-reviews
    /// </summary>
    [ApiController]
    [Route("api/admin/listing-reviews")]
    [Authorize(Roles = "Admin,SuperAdmin,ADMIN,SUPER_ADMIN,admin,superadmin")]
    public class ListingReviewController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ListingReviewController> _logger;

        public ListingReviewController(AppDbContext context, ILogger<ListingReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/listing-reviews
        // Returns a filtered list of all vendor service listings.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        [ProducesResponseType(typeof(List<ListingSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListings(
            [FromQuery] string? status,
            [FromQuery] string? category,
            [FromQuery] string? search)
        {
            try
            {
                var query = _context.VendorServices
                    .Include(vs => vs.Vendor)
                    .Include(vs => vs.Category)
                    .AsQueryable();

                // Filter by status (Draft / Pending / Active / Inactive)
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    query = query.Where(vs => vs.Status == status);
                }

                // Filter by category name
                if (!string.IsNullOrWhiteSpace(category) && category != "All")
                {
                    query = query.Where(vs => vs.Category != null && vs.Category.CategoryName == category);
                }

                // Search by vendor business name or city/address
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.ToLower();
                    query = query.Where(vs =>
                        vs.Vendor != null && (
                            vs.Vendor.BusinessName.ToLower().Contains(q) ||
                            (vs.Vendor.City != null && vs.Vendor.City.ToLower().Contains(q)) ||
                            (vs.Vendor.Address != null && vs.Vendor.Address.ToLower().Contains(q))
                        )
                    );
                }

                var items = await query
                    .OrderByDescending(vs => vs.CreatedAt)
                    .ToListAsync();

                var listings = items.Select(vs => MapToSummary(vs)).ToList();

                return Ok(listings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetListings failed");
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/listing-reviews/metrics
        // Returns counts of listings grouped by status.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("metrics")]
        public async Task<IActionResult> GetListingMetrics()
        {
            try
            {
                var total = await _context.VendorServices.CountAsync();
                var active = await _context.VendorServices.CountAsync(vs => vs.Status == "Active");
                var pending = await _context.VendorServices.CountAsync(vs => vs.Status == "Pending" || vs.Status == "Draft");
                var inactive = await _context.VendorServices.CountAsync(vs => vs.Status == "Inactive" || vs.Status == "Rejected");

                return Ok(new
                {
                    totalListings = total,
                    activeListings = active,
                    pendingReviews = pending,
                    inactiveListings = inactive,
                    total = total,
                    active = active,
                    pending = pending,
                    inactive = inactive,
                    draft = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetListingMetrics failed");
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/listing-reviews/{id}
        // Returns full detail for one listing, including vendor info,
        // category-specific attributes, and gallery images.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ListingDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetListingById(int id)
        {
            try
            {
                var vs = await _context.VendorServices
                    .Include(s => s.Vendor).ThenInclude(v => v!.User)
                    .Include(s => s.Category)
                    .Include(s => s.Images)
                    .Include(s => s.CateringDetails)
                    .Include(s => s.DecorationsDetails)
                    .Include(s => s.HotelVenueDetails)
                    .Include(s => s.MusicDetails)
                    .Include(s => s.PhotographyDetails)
                    .FirstOrDefaultAsync(s => s.ServiceId == id);

                if (vs == null)
                    return NotFound(new { message = $"Listing with ID {id} not found." });

                var detail = MapToDetail(vs);
                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetListingById failed for ID {Id}", id);
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/listing-reviews/{id}/approve
        // Sets the listing status to "Active" and marks the vendor as approved.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveListing(int id)
        {
            try
            {
                var vs = await _context.VendorServices
                    .Include(s => s.Vendor)
                    .FirstOrDefaultAsync(s => s.ServiceId == id);

                if (vs == null)
                    return NotFound(new { message = $"Listing with ID {id} not found." });

                vs.Status = "Active";

                // Also mark the vendor profile as approved if not already
                if (vs.Vendor != null)
                {
                    vs.Vendor.IsApproved = true;
                    if (vs.Vendor.Status == "Pending")
                        vs.Vendor.Status = "Approved";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Listing {Id} approved.", id);
                return Ok(new { message = "Listing approved successfully.", listingId = id, status = "Active" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveListing failed for ID {Id}", id);
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/listing-reviews/{id}/reject
        // Sets the listing status to "Inactive". Accepts an optional rejection reason.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectListing(int id, [FromBody] UpdateListingStatusDto? dto)
        {
            try
            {
                var vs = await _context.VendorServices
                    .Include(s => s.Vendor)
                    .FirstOrDefaultAsync(s => s.ServiceId == id);

                if (vs == null)
                    return NotFound(new { message = $"Listing with ID {id} not found." });

                vs.Status = "Inactive";

                // Update vendor status to Rejected if specified
                if (vs.Vendor != null && vs.Vendor.Status != "Rejected")
                    vs.Vendor.Status = "Rejected";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Listing {Id} rejected. Reason: {Reason}", id, dto?.RejectionReason ?? "none");
                return Ok(new
                {
                    message = "Listing rejected.",
                    listingId = id,
                    status = "Inactive",
                    rejectionReason = dto?.RejectionReason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RejectListing failed for ID {Id}", id);
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/listing-reviews/{id}/toggle-status
        // Toggles listing status between "Active" and "Inactive".
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}/toggle-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var vs = await _context.VendorServices
                    .FirstOrDefaultAsync(s => s.ServiceId == id);

                if (vs == null)
                    return NotFound(new { message = $"Listing with ID {id} not found." });

                vs.Status = vs.Status == "Active" ? "Inactive" : "Active";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Listing {Id} toggled to {Status}.", id, vs.Status);
                return Ok(new { message = $"Listing status set to {vs.Status}.", listingId = id, status = vs.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleStatus failed for ID {Id}", id);
                return Problem(detail: ex.Message, statusCode: 500, title: "Server Error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private mapping helpers
        // ─────────────────────────────────────────────────────────────────────

        private static ListingSummaryDto MapToSummary(VendorService vs)
        {
            var vendor   = vs.Vendor;
            var location = vendor?.City ?? vendor?.Address ?? "";
            var name     = vendor?.BusinessName ?? "Unknown";
            var initials = name.Length >= 2
                ? (name.Split(' ') is { Length: > 1 } parts
                    ? $"{parts[0][0]}{parts[1][0]}"
                    : name[..2])
                : name;

            return new ListingSummaryDto
            {
                ListingId        = vs.ServiceId,
                VendorId         = vs.VendorId,
                VendorName       = name,
                Category         = vs.Category?.CategoryName ?? "Uncategorized",
                Location         = location,
                Price            = vs.Price,
                IsPriceOnRequest = vs.IsPriceOnRequest,
                PriceDisplay     = vs.IsPriceOnRequest ? "Price on Request"
                                    : vs.Price.HasValue ? $"LKR {vs.Price:N0}" : "N/A",
                Rating           = null, // extend when a reviews table exists
                ListedDate       = vs.CreatedAt.ToString("MMM dd, yyyy"),
                Status           = vs.Status,
                CoverImageUrl    = vs.CoverImageUrl,
                Initials         = initials.ToUpper(),
            };
        }

        private static ListingDetailDto MapToDetail(VendorService vs)
        {
            var summary = MapToSummary(vs);
            var vendor  = vs.Vendor;
            var user    = vendor?.User;

            var detail = new ListingDetailDto
            {
                // Copy summary fields
                ListingId        = summary.ListingId,
                VendorId         = summary.VendorId,
                VendorName       = summary.VendorName,
                Category         = summary.Category,
                Location         = summary.Location,
                Price            = summary.Price,
                IsPriceOnRequest = summary.IsPriceOnRequest,
                PriceDisplay     = summary.PriceDisplay,
                Rating           = summary.Rating,
                ListedDate       = summary.ListedDate,
                Status           = summary.Status,
                CoverImageUrl    = summary.CoverImageUrl,
                Initials         = summary.Initials,

                // Extended vendor info
                VendorEmail      = vendor?.Email ?? user?.Email,
                VendorPhone      = vendor?.ContactNumber ?? user?.PhoneNumber,
                Address          = vendor?.Address,
                City             = vendor?.City,
                State            = vendor?.State,
                Description      = vendor?.Description,
                ServiceName      = vs.ServiceName,
                ServiceDescription = vs.Description,
                IsVendorApproved = vendor?.IsApproved ?? false,

                // Gallery images from VendorServiceImages
                GalleryImages = vs.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .ToList(),

                // Category-specific details
                CategoryDetails  = BuildCategoryDetails(vs),
            };

            return detail;
        }

        private static CategoryDetailsDto? BuildCategoryDetails(VendorService vs)
        {
            // Hotel / Venue
            if (vs.HotelVenueDetails != null)
            {
                return new CategoryDetailsDto
                {
                    VenueType         = vs.HotelVenueDetails.VenueType,
                    VenueSetting      = vs.HotelVenueDetails.VenueSetting,
                    IndoorOutdoor     = vs.HotelVenueDetails.IndoorOutdoor,
                    MinimumGuestCount = vs.HotelVenueDetails.MinimumGuestCount,
                    CancellationPolicy = vs.HotelVenueDetails.CancellationPolicy,
                };
            }

            // Photography
            if (vs.PhotographyDetails != null)
            {
                return new CategoryDetailsDto
                {
                    PhotographyStyle = vs.PhotographyDetails.ShootingStyle,
                    PackageHours     = vs.PhotographyDetails.HoursOfCoverage != null
                                        ? int.TryParse(vs.PhotographyDetails.HoursOfCoverage, out var h) ? h : null
                                        : null,
                };
            }

            // Music
            if (vs.MusicDetails != null)
            {
                return new CategoryDetailsDto
                {
                    MusicGenres     = vs.MusicDetails.Genres != null ? string.Join(", ", vs.MusicDetails.Genres) : null,
                    PerformanceType = vs.MusicDetails.PerformanceType,
                };
            }

            // Decorations
            if (vs.DecorationsDetails != null)
            {
                return new CategoryDetailsDto
                {
                    DecorationStyles = vs.DecorationsDetails.PrimaryStyles != null ? string.Join(", ", vs.DecorationsDetails.PrimaryStyles) : null,
                    SetupTime        = vs.DecorationsDetails.SetupTimeRequired,
                };
            }

            // Catering
            if (vs.CateringDetails != null)
            {
                return new CategoryDetailsDto
                {
                    CuisineType   = vs.CateringDetails.Cuisines != null ? string.Join(", ", vs.CateringDetails.Cuisines) : null,
                    GuestCapacity = vs.CateringDetails.MaxGuests,
                };
            }

            return null;
        }
    }
}
