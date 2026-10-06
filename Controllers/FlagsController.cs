using Backend.Constants;
using Backend.Data;
using Backend.DTOs;
using Backend.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    /// <summary>
    /// Manages the Flag/Report Listing feature across mobile and web dashboards.
    ///
    /// Endpoints:
    ///   POST   POST /api/flags         – Customer submits a new flag (mobile)
    ///   GET    /api/flags/check        – Checks if a user already flagged a listing (mobile)
    ///   GET    /api/flags/admin        – Admin view: all flags with reporter/vendor details
    ///   GET    /api/flags/vendor/{id}  – Vendor view: flags against that vendor's listings
    ///   GET    /api/flags/vendor/my-flags – Authenticated vendor view
    ///   PATCH  /api/flags/{id}/status  – Admin updates flag status (dismiss / remove / review)
    /// </summary>
    [ApiController]
    [Route("api/flags")]
    public class FlagsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public FlagsController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // POST /api/flags
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Creates a new flag. Called by the Flutter mobile app after the customer
        /// fills in the report bottom sheet.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFlagDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Prevent duplicate flags from the same user for the same listing
            var existing = await _context.FlaggedItems
                .FirstOrDefaultAsync(f => f.ListingId == dto.ListingId && f.ReporterUserId == dto.ReporterUserId);

            if (existing != null)
                return Conflict(new { message = "You have already reported this listing." });

            // Resolve the listing title and vendor ID if not provided
            string contentTitle = dto.ContentTitle;
            int resolvedVendorId = dto.VendorId;

            if (dto.ListingId > 0)
            {
                var listing = await _context.VendorServices
                    .Where(vs => vs.ServiceId == dto.ListingId)
                    .Select(vs => new { vs.ServiceName, vs.VendorId })
                    .FirstOrDefaultAsync();
                if (listing != null)
                {
                    if (string.IsNullOrWhiteSpace(contentTitle))
                        contentTitle = listing.ServiceName;
                    if (resolvedVendorId <= 0)
                        resolvedVendorId = listing.VendorId;
                }
                else if (string.IsNullOrWhiteSpace(contentTitle))
                {
                    contentTitle = "Unknown Listing";
                }
            }

            var vendorRecord = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == resolvedVendorId || v.UserId == resolvedVendorId);
            if (vendorRecord != null)
            {
                resolvedVendorId = vendorRecord.VendorId;
            }

            // Derive severity from reason if not set explicitly
            string severity = dto.Severity;
            if (string.IsNullOrWhiteSpace(severity) || severity == "Medium")
            {
                severity = dto.Reason switch
                {
                    "Abusive language" => "High",
                    "Off-platform solicitation" => "High",
                    "Fake photos" => "Medium",
                    "Misleading price and photos" => "Medium",
                    "Duplicate listing" => "Low",
                    _ => "Medium"
                };
            }

            var flag = new FlaggedItem
            {
                ListingId = dto.ListingId,
                ReporterUserId = dto.ReporterUserId,
                VendorId = resolvedVendorId,
                ContentType = dto.ContentType,
                ContentTitle = contentTitle,
                Reason = dto.Reason,
                Severity = severity,
                Comments = dto.Comments,
                Status = "Open"
            };

            _context.FlaggedItems.Add(flag);
            await _context.SaveChangesAsync();

            // ── Send Notification to Vendor & Admins ──
            if (vendorRecord != null)
            {
                string vendorMessage = $"Your listing '{contentTitle}' was reported for: {dto.Reason}." +
                    (string.IsNullOrWhiteSpace(dto.Comments) ? "" : $" Customer message: \"{dto.Comments}\"");

                _context.Notifications.Add(new Entities.Notification
                {
                    UserId = vendorRecord.UserId,
                    Type = NotificationTypes.ListingFlagged,
                    Title = "Listing Reported",
                    Message = vendorMessage,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            try
            {
                await _notificationService.CreateForAllAdminsAsync(
                    NotificationTypes.ListingFlagged,
                    "Listing Flagged",
                    $"Listing '{contentTitle}' was flagged for: {dto.Reason}.");
            }
            catch
            {
                // Non-blocking if admin notification dispatch encounters preference edge case
            }

            return Created($"/api/flags/{flag.Id}", new { flag.Id, flag.Status });
        }

        // ──────────────────────────────────────────────────────────────────────────
        // GET /api/flags/check?listingId={id}&userId={id}
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns whether a specific user has already flagged a specific listing.
        /// Called on page load in ListingDetailsScreen to update button state.
        /// </summary>
        [HttpGet("check")]
        public async Task<ActionResult<FlagCheckDto>> Check(
            [FromQuery] int listingId,
            [FromQuery] int userId)
        {
            var existing = await _context.FlaggedItems
                .Where(f => f.ListingId == listingId && f.ReporterUserId == userId)
                .Select(f => new { f.Id })
                .FirstOrDefaultAsync();

            return Ok(new FlagCheckDto
            {
                HasReported = existing != null,
                ExistingFlagId = existing?.Id
            });
        }

        // ──────────────────────────────────────────────────────────────────────────
        // GET /api/flags/admin
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all flagged items enriched with reporter name/email and vendor name.
        /// Consumed by the Admin Dashboard FlaggedContentPage.
        /// </summary>
        [HttpGet("admin")]
        public async Task<ActionResult<List<FlaggedItemDto>>> GetAllForAdmin()
        {
            var flags = await _context.FlaggedItems
                .Include(f => f.Reporter)
                .Include(f => f.Vendor)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var result = flags.Select(f => new FlaggedItemDto
            {
                Id = f.Id,
                ListingId = f.ListingId,
                ReporterUserId = f.ReporterUserId,
                VendorId = f.VendorId,
                ContentType = f.ContentType,
                ContentTitle = f.ContentTitle,
                Reason = f.Reason,
                Severity = f.Severity,
                Comments = f.Comments,
                Status = f.Status,
                ResolutionNote = f.ResolutionNote,
                ReporterName = f.Reporter?.FullName ?? f.Reporter?.Email ?? "Anonymous",
                ReporterEmail = f.Reporter?.Email ?? string.Empty,
                VendorName = f.Vendor?.BusinessName ?? $"Vendor #{f.VendorId}",
                CreatedAt = f.CreatedAt,
                ReviewedAt = f.ReviewedAt
            }).ToList();

            return Ok(result);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Vendor resolution helper
        // ──────────────────────────────────────────────────────────────────────────
        private async Task<Vendor?> ResolveVendorAsync(int? vendorOrUserId)
        {
            // 1. Try resolving from authenticated JWT claims first if caller is logged in
            var vendorIdClaim = User.FindFirst("vendorId")?.Value
                ?? User.FindFirst("VendorId")?.Value;
            if (int.TryParse(vendorIdClaim, out var claimVendorId) && claimVendorId > 0)
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == claimVendorId);
                if (v != null) return v;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out var claimUserId) && claimUserId > 0)
            {
                var v = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == claimUserId);
                if (v != null) return v;
            }

            // 2. If vendorOrUserId parameter is provided:
            if (vendorOrUserId.HasValue && vendorOrUserId.Value > 0)
            {
                int id = vendorOrUserId.Value;
                // Check if id matches UserId first (since the frontend frequently passes userId)
                var vByUser = await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == id);
                if (vByUser != null) return vByUser;

                // Otherwise check VendorId
                var vByVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
                if (vByVendor != null) return vByVendor;
            }

            return null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // GET /api/flags/vendor/{vendorId}
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all flags targeting the given vendor's listings.
        /// Resolves whether the provided ID is a VendorId or UserId.
        /// Consumed by the Vendor Dashboard to show customer complaints.
        /// </summary>
        [HttpGet("vendor/{vendorId:int}")]
        public async Task<ActionResult<List<FlaggedItemDto>>> GetForVendor(int vendorId)
        {
            var vendor = await ResolveVendorAsync(vendorId);
            int resolvedVendorId = vendor?.VendorId ?? vendorId;

            var flags = await _context.FlaggedItems
                .Include(f => f.Reporter)
                .Where(f => f.VendorId == resolvedVendorId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var result = flags.Select(f => new FlaggedItemDto
            {
                Id = f.Id,
                ListingId = f.ListingId,
                ReporterUserId = f.ReporterUserId,
                VendorId = f.VendorId,
                ContentType = f.ContentType,
                ContentTitle = f.ContentTitle,
                Reason = f.Reason,
                Severity = f.Severity,
                Comments = f.Comments,
                Status = f.Status,
                ResolutionNote = f.ResolutionNote,
                ReporterName = f.Reporter?.FullName ?? "Anonymous",
                ReporterEmail = f.Reporter?.Email ?? string.Empty,
                VendorName = vendor?.BusinessName ?? string.Empty,
                CreatedAt = f.CreatedAt,
                ReviewedAt = f.ReviewedAt
            }).ToList();

            return Ok(result);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // GET /api/flags/vendor/my-flags
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Resolves flags for the currently authenticated vendor via JWT token claims.
        /// </summary>
        [HttpGet("vendor/my-flags")]
        public async Task<ActionResult<List<FlaggedItemDto>>> GetMyFlags()
        {
            var vendor = await ResolveVendorAsync(null);
            if (vendor == null)
            {
                return Ok(new List<FlaggedItemDto>());
            }

            return await GetForVendor(vendor.VendorId);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // PATCH /api/flags/{id}/status
        // ──────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Admin updates a flag's status (Open → UnderReview → Dismissed / ContentRemoved).
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFlagStatusDto dto)
        {
            var flag = await _context.FlaggedItems.FindAsync(id);
            if (flag == null)
                return NotFound();

            flag.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.ResolutionNote))
                flag.ResolutionNote = dto.ResolutionNote;

            if (dto.Status is "Dismissed" or "ContentRemoved")
                flag.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { flag.Id, flag.Status, flag.ReviewedAt });
        }
    }
}
