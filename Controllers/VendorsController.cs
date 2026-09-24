using Backend.Data;
using Backend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class VendorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(AppDbContext context, ILogger<VendorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/vendors
        /// Returns a list of public vendors / services for mobile and web explore screens.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVendors(
            [FromQuery] string? category,
            [FromQuery] string? search)
        {
            try
            {
                var vendorsQuery = _context.Vendors
                    .Include(v => v.VendorServices)
                        .ThenInclude(vs => vs.Category)
                    .Where(v => v.Status == "Approved" || v.Status == "Active" || v.IsApproved);

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    vendorsQuery = vendorsQuery.Where(v =>
                        (v.Category != null && v.Category.ToLower() == category.ToLower()) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.ToLower() == category.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    vendorsQuery = vendorsQuery.Where(v =>
                        v.BusinessName.ToLower().Contains(s) ||
                        (v.City != null && v.City.ToLower().Contains(s)) ||
                        (v.Category != null && v.Category.ToLower().Contains(s)));
                }

                var vendors = await vendorsQuery.ToListAsync();

                var result = vendors.Select(v =>
                {
                    var primaryService = v.VendorServices.FirstOrDefault();
                    var categoryName = v.Category ?? primaryService?.Category?.CategoryName ?? "General";
                    var minPrice = v.VendorServices.Where(s => s.Price.HasValue).Select(s => s.Price!.Value).DefaultIfEmpty(0).Min();

                    return new
                    {
                        id = v.VendorId.ToString(),
                        vendorId = v.VendorId,
                        name = v.BusinessName,
                        businessName = v.BusinessName,
                        category = categoryName,
                        categoryIcon = GetCategoryIcon(categoryName),
                        priceFrom = (double)minPrice,
                        rating = 4.8,
                        reviewCount = 15,
                        imageUrl = v.CoverImageUrl ?? primaryService?.CoverImageUrl ?? v.LogoUrl ?? "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80",
                        coverImageUrl = v.CoverImageUrl,
                        logoUrl = v.LogoUrl,
                        location = v.City ?? v.Address ?? "Sri Lanka",
                        city = v.City,
                        description = v.Description ?? "Quality wedding vendor services.",
                        isFeatured = v.IsApproved
                    };
                }).ToList();

                // If no vendors matched active/approved status directly, query available vendor services so public users always see live catalog
                if (result.Count == 0)
                {
                    var services = await _context.VendorServices
                        .Include(vs => vs.Category)
                        .Include(vs => vs.Vendor)
                        .Take(30)
                        .ToListAsync();

                    result = services.Select(vs => new
                    {
                        id = vs.ServiceId.ToString(),
                        vendorId = vs.VendorId,
                        name = vs.ServiceName,
                        businessName = vs.Vendor?.BusinessName ?? vs.ServiceName,
                        category = vs.Category?.CategoryName ?? "General",
                        categoryIcon = GetCategoryIcon(vs.Category?.CategoryName),
                        priceFrom = (double)(vs.Price ?? 0),
                        rating = 4.8,
                        reviewCount = 10,
                        imageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80",
                        coverImageUrl = vs.CoverImageUrl,
                        logoUrl = vs.Vendor?.LogoUrl,
                        location = vs.Vendor?.City ?? vs.Vendor?.Address ?? "Sri Lanka",
                        city = vs.Vendor?.City,
                        description = vs.ShortDescription ?? "Exquisite wedding service.",
                        isFeatured = true
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public vendors");
                return Problem(
                    detail: "An error occurred while fetching vendors.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendorById(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.VendorServices)
                    .ThenInclude(vs => vs.Category)
                .FirstOrDefaultAsync(v => v.VendorId == id);

            if (vendor == null) return NotFound(new { message = "Vendor not found" });

            return Ok(vendor);
        }

        private static string GetCategoryIcon(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "sparkles";
            var cat = category.ToLower();
            if (cat.Contains("venue") || cat.Contains("hotel")) return "location_city";
            if (cat.Contains("photo") || cat.Contains("video")) return "camera_alt";
            if (cat.Contains("music") || cat.Contains("dj") || cat.Contains("band")) return "music_note";
            if (cat.Contains("cater") || cat.Contains("food")) return "restaurant";
            if (cat.Contains("decor") || cat.Contains("flower")) return "local_florist";
            if (cat.Contains("attire") || cat.Contains("dress")) return "checkroom";
            return "stars";
        }
    }
}
