using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class VendorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IVendorRatingService _ratingService;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(
            AppDbContext context,
            IVendorRatingService ratingService,
            ILogger<VendorsController> logger)
        {
            _context = context;
            _ratingService = ratingService;
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
                    var catLower = category.Trim().ToLower();
                    vendorsQuery = vendorsQuery.Where(v =>
                        (v.Category != null && v.Category.ToLower().Contains(catLower)) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.ToLower() == catLower));
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

                // Post-query exact token membership check for CSV categories
                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var catTrimmed = category.Trim();
                    vendors = vendors.Where(v =>
                        (v.Category != null && v.Category
                            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                            .Any(c => c.Equals(catTrimmed, StringComparison.OrdinalIgnoreCase))) ||
                        v.VendorServices.Any(vs => vs.Category != null && vs.Category.CategoryName.Equals(catTrimmed, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var vendorIds = vendors.Select(v => v.VendorId).ToList();
                var ratingSummaries = await _ratingService.GetVendorsRatingSummariesAsync(vendorIds);

                var result = vendors.Select(v =>
                {
                    var primaryService = v.VendorServices.FirstOrDefault();
                    var categoryName = v.Category ?? primaryService?.Category?.CategoryName ?? "General";
                    var minPrice = v.VendorServices.Where(s => s.Price.HasValue).Select(s => s.Price!.Value).DefaultIfEmpty(0).Min();

                    var hasRating = ratingSummaries.TryGetValue(v.VendorId, out var rStat);
                    var avgRating = hasRating && rStat!.TotalRatings > 0 ? rStat.AverageRating : 0.0;
                    var totalCount = hasRating ? rStat!.TotalRatings : 0;

                    return new
                    {
                        id = v.VendorId.ToString(),
                        vendorId = v.VendorId,
                        name = v.BusinessName,
                        businessName = v.BusinessName,
                        category = categoryName,
                        categoryIcon = GetCategoryIcon(categoryName),
                        priceFrom = (double)minPrice,
                        rating = avgRating,
                        reviewCount = totalCount,
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

                    var sVendorIds = services.Where(s => s.VendorId > 0).Select(s => s.VendorId).Distinct().ToList();
                    var sRatingSummaries = await _ratingService.GetVendorsRatingSummariesAsync(sVendorIds);

                    result = services.Select(vs =>
                    {
                        var hasRating = sRatingSummaries.TryGetValue(vs.VendorId, out var rStat);
                        var avgRating = hasRating && rStat!.TotalRatings > 0 ? rStat.AverageRating : 0.0;
                        var totalCount = hasRating ? rStat!.TotalRatings : 0;

                        return new
                        {
                            id = vs.ServiceId.ToString(),
                            vendorId = vs.VendorId,
                            name = vs.ServiceName,
                            businessName = vs.Vendor?.BusinessName ?? vs.ServiceName,
                            category = vs.Category?.CategoryName ?? "General",
                            categoryIcon = GetCategoryIcon(vs.Category?.CategoryName),
                            priceFrom = (double)(vs.Price ?? 0),
                            rating = avgRating,
                            reviewCount = totalCount,
                            imageUrl = vs.CoverImageUrl ?? vs.Vendor?.CoverImageUrl ?? "https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80",
                            coverImageUrl = vs.CoverImageUrl,
                            logoUrl = vs.Vendor?.LogoUrl,
                            location = vs.Vendor?.City ?? vs.Vendor?.Address ?? "Sri Lanka",
                            city = vs.Vendor?.City,
                            description = vs.ShortDescription ?? "Exquisite wedding service.",
                            isFeatured = true
                        };
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

        /// <summary>
        /// GET /api/vendors/{id}
        /// Public vendor profile for customers: business details, hours, and past performances.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVendorById(int id)
        {
            try
            {
                var vendor = await _context.Vendors
                    .Include(v => v.GalleryImages)
                    .Include(v => v.VendorServices)
                        .ThenInclude(vs => vs.Category)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VendorId == id);

                if (vendor == null)
                {
                    return NotFound(new { message = "Vendor not found" });
                }

                var performances = await _context.VendorPerformances
                    .AsNoTracking()
                    .Where(p => p.VendorId == id)
                    .OrderByDescending(p => p.EventDate)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var ratingSummary = await _ratingService.GetVendorRatingSummaryAsync(id);
                var isAuthenticated = User.Identity?.IsAuthenticated == true;
                return Ok(MapPublicProfile(vendor, performances, ratingSummary, isAuthenticated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public vendor profile {VendorId}", id);
                return Problem(
                    detail: "An error occurred while fetching the vendor profile.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        private static string GetCategoryIcon(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "sparkles";
            var first = category.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? category;
            var cat = first.ToLower();
            if (cat.Contains("venue") || cat.Contains("hotel")) return "location_city";
            if (cat.Contains("photo") || cat.Contains("video")) return "camera_alt";
            if (cat.Contains("music") || cat.Contains("dj") || cat.Contains("band")) return "music_note";
            if (cat.Contains("cater") || cat.Contains("food")) return "restaurant";
            if (cat.Contains("decor") || cat.Contains("flower")) return "local_florist";
            if (cat.Contains("attire") || cat.Contains("dress")) return "checkroom";
            return "stars";
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static PublicVendorProfileDto MapPublicProfile(
            Vendor vendor,
            List<VendorPerformance> performances,
            VendorRatingSummaryDto? ratingSummary = null,
            bool isAuthenticated = true)
        {
            var hours = new List<BusinessHoursItemDto>();
            if (!string.IsNullOrWhiteSpace(vendor.BusinessHoursJson))
            {
                try
                {
                    hours = JsonSerializer.Deserialize<List<BusinessHoursItemDto>>(vendor.BusinessHoursJson, JsonOptions)
                            ?? new List<BusinessHoursItemDto>();
                }
                catch
                {
                    hours = new List<BusinessHoursItemDto>();
                }
            }

            var socials = new SocialLinksDto();
            if (!string.IsNullOrWhiteSpace(vendor.SocialLinksJson))
            {
                try
                {
                    socials = JsonSerializer.Deserialize<SocialLinksDto>(vendor.SocialLinksJson, JsonOptions)
                              ?? new SocialLinksDto();
                }
                catch
                {
                    socials = new SocialLinksDto();
                }
            }

            var location = string.Join(", ", new[] { vendor.City, vendor.State, vendor.Country }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (string.IsNullOrWhiteSpace(location))
            {
                location = vendor.Address ?? "Sri Lanka";
            }

            return new PublicVendorProfileDto
            {
                VendorId = vendor.VendorId,
                BusinessName = vendor.BusinessName,
                Category = vendor.Category,
                Tagline = vendor.Tagline,
                Description = vendor.Description,
                OwnerName = vendor.OwnerName,
                ContactNumber = isAuthenticated ? vendor.ContactNumber : null,
                AltPhoneNumber = isAuthenticated ? vendor.AltPhoneNumber : null,
                Email = isAuthenticated ? vendor.Email : null,
                WebsiteUrl = isAuthenticated ? vendor.WebsiteUrl : null,
                ContactHidden = isAuthenticated ? null : true,
                Address = vendor.Address,
                City = vendor.City,
                State = vendor.State,
                PostalCode = vendor.PostalCode,
                Country = vendor.Country,
                ServiceAreas = vendor.ServiceAreas,
                TravelPolicy = vendor.TravelPolicy,
                YearsInBusiness = vendor.YearsInBusiness,
                IsApproved = vendor.IsApproved,
                LogoUrl = vendor.LogoUrl,
                CoverImageUrl = vendor.CoverImageUrl,
                Location = location,
                AverageRating = ratingSummary?.AverageRating ?? 0.0,
                RatingCount = ratingSummary?.TotalRatings ?? 0,
                ReviewCount = (ratingSummary?.TotalRatings ?? 0) > 0 ? ratingSummary!.TotalRatings : performances.Count(p => !string.IsNullOrWhiteSpace(p.CustomerFeedback)),
                BusinessHours = hours,
                SocialLinks = socials,
                GalleryImages = vendor.GalleryImages
                    .OrderBy(g => g.DisplayOrder)
                    .Select(g => new VendorGalleryImageDto
                    {
                        ImageId = g.ImageId,
                        ImageUrl = g.ImageUrl,
                        Caption = g.Caption,
                        Category = g.Category,
                        DisplayOrder = g.DisplayOrder,
                        IsFeatured = g.IsFeatured
                    })
                    .ToList(),
                Performances = performances.Select(p => new VendorPerformanceResponseDto
                {
                    PerformanceId = p.PerformanceId,
                    Title = p.Title,
                    Category = p.Category,
                    Description = p.Description,
                    PhotoUrl = p.PhotoUrl,
                    CustomerName = p.CustomerName,
                    CustomerFeedback = p.CustomerFeedback,
                    EventDate = p.EventDate
                }).ToList(),
                Services = vendor.VendorServices
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new PublicVendorServiceItemDto
                    {
                        ServiceId = s.ServiceId,
                        Title = s.ServiceName,
                        Category = s.Category?.CategoryName ?? vendor.Category,
                        ShortDescription = s.ShortDescription,
                        Price = s.Price,
                        IsPriceOnRequest = s.IsPriceOnRequest,
                        CoverImageUrl = s.CoverImageUrl
                    })
                    .ToList()
            };
        }
    }
}
