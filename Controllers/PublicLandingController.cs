using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/public/landing")]
    [AllowAnonymous]
    public class PublicLandingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PublicLandingController> _logger;

        public PublicLandingController(AppDbContext context, ILogger<PublicLandingController> logger)
        {
            _context = context;
            _logger = logger;
        }

                // GET /api/public/landing/metrics
        [HttpGet("metrics")]
        public async Task<IActionResult> GetLandingMetrics()
        {
            try
            {
                // Query the actual separate service tables to get accurate counts
                var venueCount = await _context.HotelVenueDetails.CountAsync();
                var photoCount = await _context.PhotographyDetails.CountAsync();
                var musicCount = await _context.MusicDetails.CountAsync();
                var decorCount = await _context.DecorationsDetails.CountAsync();
                var cateringCount = await _context.CateringDetails.CountAsync();

                // Total can be the sum or the total from the main Vendors table
                var totalActive = await _context.Vendors.CountAsync(v => v.Status == "Active");

                return Ok(new
                {
                    totalActiveVendors = totalActive,
                    venues = venueCount,
                    photography = photoCount,
                    music = musicCount,
                    decorations = decorCount,
                    catering = cateringCount
                });
            }
            catch (Exception ex)
            {
                // Fallback to 0 if tables don't exist yet to prevent API crash
                return Ok(new { venues = 0, photography = 0, music = 0, decorations = 0, catering = 0 });
            }
        }

        // GET /api/public/landing/featured-listings
        [HttpGet("featured-listings")]
        public async Task<IActionResult> GetFeaturedListings()
        {
            try
            {
                var featured = await _context.VendorServices
                    .Include(vs => vs.Category)
                    .Include(vs => vs.Vendor)
                    .Where(vs =>
                        (vs.Status == "Active" || vs.Status == "Approved") &&
                        vs.Vendor != null &&
                        (vs.Vendor.Status == "Approved" || vs.Vendor.IsApproved))
                    .OrderByDescending(vs => vs.CreatedAt)
                    .Take(6)
                    .Select(vs => new
                    {
                        serviceId        = vs.ServiceId,
                        serviceName      = vs.ServiceName,
                        businessName     = vs.Vendor!.BusinessName,
                        category         = vs.Category!.CategoryName,
                        categoryId       = vs.CategoryId,
                        location         = vs.Vendor.City ?? vs.Vendor.Address ?? "Sri Lanka",
                        coverImageUrl    = vs.CoverImageUrl ?? vs.Vendor.CoverImageUrl,
                        logoUrl          = vs.Vendor.LogoUrl,
                        price            = vs.IsPriceOnRequest ? (decimal?)null : vs.Price,
                        isPriceOnRequest = vs.IsPriceOnRequest,
                        shortDescription = vs.ShortDescription
                    })
                    .ToListAsync();

                return Ok(featured);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured listings");
                return StatusCode(500, new { message = "Could not fetch featured listings." });
            }
        }
    }
}
