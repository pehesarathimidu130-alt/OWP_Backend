using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/vendor-dashboard")]
    public class VendorDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IVendorRatingService _ratingService;
        private readonly ILogger<VendorDashboardController> _logger;

        public VendorDashboardController(
            AppDbContext context,
            IVendorRatingService ratingService,
            ILogger<VendorDashboardController> logger)
        {
            _context = context;
            _ratingService = ratingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? userId)
        {
            try
            {
                var vendor = await ResolveVendorAsync(userId);
                if (vendor == null)
                {
                    return NotFound(new { message = "Vendor profile not found." });
                }

                // Compute real-time rating metrics via server-side query aggregation
                var ratingSummary = await _ratingService.GetVendorRatingSummaryAsync(vendor.VendorId);

                // Compute real-time listing metrics (case-insensitive)
                var activeListingsCount = await _context.VendorServices
                    .CountAsync(vs => vs.VendorId == vendor.VendorId && vs.Status != null &&
                        (vs.Status.ToLower() == "active" || vs.Status.ToLower() == "published"));
                var totalListingsCount = await _context.VendorServices
                    .CountAsync(vs => vs.VendorId == vendor.VendorId);
                var pendingListingsCount = await _context.VendorServices
                    .CountAsync(vs => vs.VendorId == vendor.VendorId && vs.Status != null &&
                        (vs.Status.ToLower() == "pending" || vs.Status.ToLower() == "underreview"));
                var draftListingsCount = await _context.VendorServices
                    .CountAsync(vs => vs.VendorId == vendor.VendorId && vs.Status != null &&
                        vs.Status.ToLower() == "draft");

                // Fetch real active services to showcase on vendor dashboard
                var activeServices = await _context.VendorServices
                    .Include(vs => vs.Category)
                    .Include(vs => vs.Images)
                    .Where(vs => vs.VendorId == vendor.VendorId && vs.Status != null &&
                        (vs.Status.ToLower() == "active" || vs.Status.ToLower() == "published"))
                    .OrderByDescending(vs => vs.UpdatedAt)
                    .Take(6)
                    .Select(vs => new
                    {
                        serviceId = vs.ServiceId,
                        id = vs.ServiceId,
                        title = vs.ServiceName,
                        category = vs.Category != null ? vs.Category.CategoryName : (vendor.Category ?? "Service"),
                        price = vs.Price,
                        priceOnRequest = vs.IsPriceOnRequest,
                        status = vs.Status,
                        coverImageUrl = vs.CoverImageUrl ?? vs.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                        description = vs.ShortDescription ?? vs.Description,
                        views = 0,
                        inquiries = 0,
                        updatedAt = vs.UpdatedAt
                    })
                    .ToListAsync();

                // Compute recent notifications for vendor
                var recentNotifications = await _context.Notifications
                    .Where(n => n.UserId == vendor.UserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        notificationId = n.NotificationId,
                        id = n.NotificationId,
                        title = n.Title,
                        message = n.Message,
                        type = n.Type,
                        isRead = n.IsRead,
                        createdAt = n.CreatedAt
                    })
                    .ToListAsync();

                // Compute inquiries
                var inquiries = await _context.VendorInquiries
                    .Where(i => i.VendorId == vendor.VendorId)
                    .Include(i => i.Customer)
                    .Include(i => i.User)
                    .Include(i => i.VendorService)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var newInquiriesCount = inquiries.Count(i => i.Status == "Pending" || i.Status == "New");

                var mappedInquiries = inquiries.Take(10).Select(i =>
                {
                    var customerName = i.Customer != null
                        ? $"{i.Customer.FirstName} {i.Customer.LastName}".Trim()
                        : (i.User?.FullName ?? "Customer");

                    var parts = customerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var initials = parts.Length >= 2
                        ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                        : (parts.Length == 1 ? parts[0][0].ToString().ToUpper() : "CU");

                    return new
                    {
                        id = i.InquiryId,
                        initials = initials,
                        customer = customerName,
                        service = i.VendorService?.ServiceName ?? vendor.Category ?? "Wedding Service",
                        eventDate = i.WeddingDate?.ToString("MMMM d, yyyy") ?? "Date TBD",
                        received = GetTimeAgo(i.CreatedAt),
                        status = i.Status == "Pending" ? "New" : i.Status,
                        statusStyle = i.Status == "Responded"
                            ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                            : "bg-[#FDF0F4] text-[#8E406F] border-[#E8C4D8]",
                        avatarBg = "bg-[#8E406F]",
                        avatarText = "text-white"
                    };
                }).ToList();

                var location = string.Join(", ", new[] { vendor.City, vendor.State, vendor.Country }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));
                if (string.IsNullOrWhiteSpace(location))
                {
                    location = vendor.Address ?? "Sri Lanka";
                }

                var response = new
                {
                    vendorId = vendor.VendorId,
                    userId = vendor.UserId,
                    businessName = vendor.BusinessName,
                    businessType = vendor.Category ?? vendor.BusinessType ?? "Wedding Vendor",
                    location = location,
                    email = vendor.Email ?? vendor.User?.Email,
                    phone = vendor.ContactNumber ?? vendor.User?.PhoneNumber,
                    isApproved = vendor.IsApproved,
                    averageRating = ratingSummary.AverageRating,
                    ratingCount = ratingSummary.TotalRatings,
                    reviewCount = ratingSummary.TotalRatings,
                    activeListings = activeListingsCount,
                    totalListings = totalListingsCount,
                    pendingListings = pendingListingsCount,
                    draftListings = draftListingsCount,
                    newInquiries = newInquiriesCount,
                    recentInquiries = mappedInquiries,
                    activeServices = activeServices,
                    recentNotifications = recentNotifications
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendor dashboard data");
                return Problem(
                    detail: "An unexpected error occurred while loading dashboard metrics.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        private async Task<Vendor?> ResolveVendorAsync(int? queryUserId)
        {
            if (queryUserId.HasValue && queryUserId.Value > 0)
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == queryUserId.Value);
                if (v != null) return v;
            }

            var vendorIdClaim = User.FindFirst("vendorId")?.Value
                ?? User.FindFirst("VendorId")?.Value
                ?? User.FindFirst("vendor_id")?.Value;

            if (int.TryParse(vendorIdClaim, out var vendorId))
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == vendorId);
                if (v != null) return v;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("id")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
                if (v != null) return v;
            }

            return await _context.Vendors.FirstOrDefaultAsync(v => v.IsApproved)
                ?? await _context.Vendors.FirstOrDefaultAsync();
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.UtcNow - dt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 2) return "Yesterday";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            return dt.ToString("MMM d, yyyy");
        }
    }
}
