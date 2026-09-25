using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ListingsController> _logger;

        public ListingsController(AppDbContext context, ILogger<ListingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/listings
        /// Returns all active business services / packages added by vendors with parent vendor info.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetListings(
            [FromQuery] string? category,
            [FromQuery] string? search)
        {
            try
            {
                var query = _context.VendorServices
                    .Include(vs => vs.Vendor)
                    .Include(vs => vs.Category)
                    .Include(vs => vs.Images)
                    .AsQueryable();

                // Optional category filter
                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var catLower = category.ToLower().Trim();
                    query = query.Where(vs =>
                        (vs.Category != null && vs.Category.CategoryName.ToLower().Contains(catLower)) ||
                        vs.ServiceName.ToLower().Contains(catLower));
                }

                // Optional search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower().Trim();
                    query = query.Where(vs =>
                        vs.ServiceName.ToLower().Contains(s) ||
                        vs.ShortDescription.ToLower().Contains(s) ||
                        (vs.Description != null && vs.Description.ToLower().Contains(s)) ||
                        (vs.Vendor != null && vs.Vendor.BusinessName.ToLower().Contains(s)) ||
                        (vs.Vendor != null && vs.Vendor.City != null && vs.Vendor.City.ToLower().Contains(s)));
                }

                var services = await query
                    .OrderByDescending(vs => vs.CreatedAt)
                    .ToListAsync();

                var result = services.Select(vs =>
                {
                    var categoryName = vs.Category?.CategoryName ?? "General";
                    var fallbackImage = "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80";

                    return new
                    {
                        id = vs.ServiceId.ToString(),
                        serviceId = vs.ServiceId,
                        title = vs.ServiceName,
                        serviceName = vs.ServiceName,
                        category = categoryName,
                        categoryId = vs.CategoryId,
                        categoryIcon = GetCategoryIcon(categoryName),
                        shortDescription = vs.ShortDescription,
                        description = !string.IsNullOrWhiteSpace(vs.Description) ? vs.Description : vs.ShortDescription,
                        price = vs.Price,
                        priceFrom = (double)(vs.Price ?? 0),
                        isPriceOnRequest = vs.IsPriceOnRequest,
                        status = vs.Status,
                        coverImageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                        imageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                        images = vs.Images.Select(img => img.ImageUrl).ToList(),

                        // Parent Vendor info
                        vendorId = vs.VendorId,
                        vendor = new
                        {
                            id = vs.Vendor?.VendorId.ToString() ?? vs.VendorId.ToString(),
                            vendorId = vs.VendorId,
                            name = vs.Vendor?.BusinessName ?? "Verified Vendor",
                            businessName = vs.Vendor?.BusinessName ?? "Verified Vendor",
                            ownerName = vs.Vendor?.OwnerName,
                            location = vs.Vendor?.City ?? vs.Vendor?.Address ?? "Sri Lanka",
                            city = vs.Vendor?.City ?? "Sri Lanka",
                            address = vs.Vendor?.Address,
                            contactNumber = vs.Vendor?.ContactNumber,
                            email = vs.Vendor?.Email,
                            logoUrl = vs.Vendor?.LogoUrl,
                            coverImageUrl = vs.Vendor?.CoverImageUrl,
                            rating = 4.9,
                            reviewCount = 18,
                            yearsInBusiness = vs.Vendor?.YearsInBusiness ?? 3,
                            isApproved = vs.Vendor?.IsApproved ?? true
                        }
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching listings");
                return Problem(
                    detail: "An error occurred while fetching business services.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        /// <summary>
        /// GET /api/listings/{id}
        /// Returns specific service details with vendor and category data.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListingById(int id)
        {
            try
            {
                var vs = await _context.VendorServices
                    .Include(s => s.Vendor)
                    .Include(s => s.Category)
                    .Include(s => s.Images)
                    .Include(s => s.HotelVenueDetails)
                    .Include(s => s.PhotographyDetails)
                    .Include(s => s.DecorationsDetails)
                    .Include(s => s.CateringDetails)
                    .Include(s => s.MusicDetails)
                    .Include(s => s.VenueSpaces)
                    .FirstOrDefaultAsync(s => s.ServiceId == id);

                if (vs == null)
                {
                    return NotFound(new { message = $"Service with ID {id} not found." });
                }

                var categoryName = vs.Category?.CategoryName ?? "General";
                var fallbackImage = "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80";

                var result = new
                {
                    id = vs.ServiceId.ToString(),
                    serviceId = vs.ServiceId,
                    title = vs.ServiceName,
                    serviceName = vs.ServiceName,
                    category = categoryName,
                    categoryId = vs.CategoryId,
                    categoryIcon = GetCategoryIcon(categoryName),
                    shortDescription = vs.ShortDescription,
                    description = !string.IsNullOrWhiteSpace(vs.Description) ? vs.Description : vs.ShortDescription,
                    price = vs.Price,
                    priceFrom = (double)(vs.Price ?? 0),
                    isPriceOnRequest = vs.IsPriceOnRequest,
                    status = vs.Status,
                    coverImageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                    imageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? fallbackImage,
                    images = vs.Images.Select(img => img.ImageUrl).ToList(),

                    vendorId = vs.VendorId,
                    vendor = new
                    {
                        id = vs.Vendor?.VendorId.ToString() ?? vs.VendorId.ToString(),
                        vendorId = vs.VendorId,
                        name = vs.Vendor?.BusinessName ?? "Verified Vendor",
                        businessName = vs.Vendor?.BusinessName ?? "Verified Vendor",
                        ownerName = vs.Vendor?.OwnerName,
                        location = vs.Vendor?.City ?? vs.Vendor?.Address ?? "Sri Lanka",
                        city = vs.Vendor?.City ?? "Sri Lanka",
                        address = vs.Vendor?.Address,
                        contactNumber = vs.Vendor?.ContactNumber,
                        email = vs.Vendor?.Email,
                        logoUrl = vs.Vendor?.LogoUrl,
                        coverImageUrl = vs.Vendor?.CoverImageUrl,
                        rating = 4.9,
                        reviewCount = 18,
                        yearsInBusiness = vs.Vendor?.YearsInBusiness ?? 3,
                        isApproved = vs.Vendor?.IsApproved ?? true
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching listing {Id}", id);
                return Problem(
                    detail: "An error occurred while fetching the service detail.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        private static string GetCategoryIcon(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "sparkles";
            var cat = category.ToLower();
            if (cat.Contains("venue") || cat.Contains("hotel")) return "location_city";
            if (cat.Contains("photo") || cat.Contains("video")) return "camera_alt";
            if (cat.Contains("music") || cat.Contains("dj") || cat.Contains("band")) return "music_note";
            if (cat.Contains("cater") || cat.Contains("food")) return "restaurant";
            if (cat.Contains("decor") || cat.Contains("flower") || cat.Contains("flora")) return "local_florist";
            if (cat.Contains("attire") || cat.Contains("dress")) return "checkroom";
            return "stars";
        }

        /// <summary>
        /// POST /api/listings/{vendorServiceId}/record-view
        /// Logs a view entry for the listing. Allows both anonymous visits and authenticated users.
        /// </summary>
        [HttpPost("{vendorServiceId:int}/record-view")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RecordViewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecordView(
            [FromRoute] int vendorServiceId,
            [FromQuery] string? source = null,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecordViewRequestDto? request = null)
        {
            var listing = await _context.VendorServices
                .FirstOrDefaultAsync(vs => vs.ServiceId == vendorServiceId);

            if (listing == null || string.Equals(listing.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = $"Listing with ID {vendorServiceId} not found or is no longer available." });
            }

            var userId = GetCurrentUserId();
            var resolvedSource = !string.IsNullOrWhiteSpace(request?.Source)
                ? request.Source.Trim()
                : (!string.IsNullOrWhiteSpace(source) ? source.Trim() : null);

            if (resolvedSource != null && resolvedSource.Length > 30)
            {
                resolvedSource = resolvedSource.Substring(0, 30);
            }

            try
            {
                var view = new ListingView
                {
                    ServiceId = vendorServiceId,
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow,
                    Source = resolvedSource
                };

                _context.ListingViews.Add(view);
                await _context.SaveChangesAsync();

                return Ok(new RecordViewResponseDto(
                    Success: true,
                    ListingId: vendorServiceId,
                    ViewedAt: view.ViewedAt,
                    IsAnonymous: userId == null,
                    Message: "Listing view recorded successfully."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording view for service {ServiceId}", vendorServiceId);
                return Problem(
                    detail: "An error occurred while recording the listing view.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        /// <summary>
        /// POST /api/listings/{vendorServiceId}/toggle-favorite
        /// Toggles favorite status for this listing by an authenticated customer.
        /// </summary>
        [HttpPost("{vendorServiceId:int}/toggle-favorite")]
        [Authorize]
        [ProducesResponseType(typeof(ToggleFavoriteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleFavorite([FromRoute] int vendorServiceId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Authentication is required to favorite listings." });
            }

            var listing = await _context.VendorServices
                .FirstOrDefaultAsync(vs => vs.ServiceId == vendorServiceId);

            if (listing == null || string.Equals(listing.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = $"Listing with ID {vendorServiceId} not found or is no longer available." });
            }

            try
            {
                var existing = await _context.CustomerFavorites
                    .FirstOrDefaultAsync(cf => cf.UserId == userId.Value && cf.ServiceId == vendorServiceId);

                if (existing != null)
                {
                    _context.CustomerFavorites.Remove(existing);
                    await _context.SaveChangesAsync();

                    return Ok(new ToggleFavoriteResponseDto(
                        IsFavorite: false,
                        ListingId: vendorServiceId,
                        Message: "Listing removed from favorites."
                    ));
                }
                else
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                    var newFav = new CustomerFavorite
                    {
                        UserId = userId.Value,
                        CustomerId = customer?.CustomerId,
                        ServiceId = vendorServiceId
                    };

                    _context.CustomerFavorites.Add(newFav);
                    await _context.SaveChangesAsync();

                    return Ok(new ToggleFavoriteResponseDto(
                        IsFavorite: true,
                        ListingId: vendorServiceId,
                        Message: "Listing added to favorites."
                    ));
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate favorite or concurrency conflict for user {UserId} and service {ServiceId}", userId, vendorServiceId);
                return Ok(new ToggleFavoriteResponseDto(
                    IsFavorite: true,
                    ListingId: vendorServiceId,
                    Message: "Listing is already in favorites."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for user {UserId} and service {ServiceId}", userId, vendorServiceId);
                return Problem(
                    detail: "An error occurred while updating favorites.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("id")?.Value;

            if (int.TryParse(claim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
