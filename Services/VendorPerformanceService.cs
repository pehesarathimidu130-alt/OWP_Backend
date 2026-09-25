using Backend.Data;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class VendorPerformanceService : IVendorPerformanceService
    {
        private readonly AppDbContext _context;

        // The four statuses that currently exist on VendorService.Status.
        // Do NOT add Rejected/Flagged until that admin feature ships and the
        // field location is confirmed.
        private static readonly string[] KnownStatuses =
            ["Draft", "Pending", "Active", "Inactive"];

        public VendorPerformanceService(AppDbContext context)
        {
            _context = context;
        }

        // ─── Helper: resolve VendorId from UserId ─────────────────────────────
        // Matches the identical pattern used in VendorContentService.
        public async Task<int> GetVendorIdAsync(int userId)
        {
            var vendorId = await _context.Vendors
                .Where(v => v.UserId == userId)
                .Select(v => (int?)v.VendorId)
                .FirstOrDefaultAsync();

            return vendorId ?? throw new KeyNotFoundException("Vendor profile was not found for this user.");
        }

        // ─── 1. Active Listings Summary ───────────────────────────────────────

        public async Task<ActiveListingsSummaryDto> GetActiveListingsSummaryAsync(int vendorId)
        {
            // Single grouped query — no per-listing loop.
            var rawCounts = await _context.VendorServices
                .Where(vs => vs.VendorId == vendorId)
                .GroupBy(vs => vs.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Build a result for every known status, defaulting missing ones to 0.
            var byStatus = KnownStatuses
                .Select(s => new ListingStatusCountDto(
                    s,
                    rawCounts.FirstOrDefault(r => r.Status == s)?.Count ?? 0))
                .ToList();

            return new ActiveListingsSummaryDto(
                TotalListings: rawCounts.Sum(r => r.Count),
                ByStatus: byStatus
            );
        }

        // ─── 2. Traffic Summary ───────────────────────────────────────────────

        public async Task<TrafficSummaryDto> GetTrafficSummaryAsync(
            int vendorId, DateTime from, DateTime to)
        {
            // Normalise to UTC so the timestamp with time zone comparisons are consistent.
            var utcFrom = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
            var utcTo   = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc); // exclusive upper bound

            // Collect ServiceIds for this vendor once — used in subsequent filters.
            var serviceIds = await _context.VendorServices
                .Where(vs => vs.VendorId == vendorId)
                .Select(vs => vs.ServiceId)
                .ToListAsync();

            if (serviceIds.Count == 0)
            {
                return new TrafficSummaryDto(0, 0,
                    Array.Empty<DailyViewBucketDto>(),
                    Array.Empty<ListingTrafficDto>());
            }

            // ── View counts per listing ──────────────────────────────────────
            var viewsPerListing = await _context.ListingViews
                .Where(lv => serviceIds.Contains(lv.ServiceId)
                          && lv.ViewedAt >= utcFrom
                          && lv.ViewedAt <  utcTo)
                .GroupBy(lv => lv.ServiceId)
                .Select(g => new { ServiceId = g.Key, Count = g.Count() })
                .ToListAsync();

            // ── Favourite counts per listing ─────────────────────────────────
            var favoritesPerListing = await _context.CustomerFavorites
                .Where(cf => serviceIds.Contains(cf.ServiceId))
                .GroupBy(cf => cf.ServiceId)
                .Select(g => new { ServiceId = g.Key, Count = g.Count() })
                .ToListAsync();

            // ── Daily view buckets (for the line chart) ──────────────────────
            // EF Core / Npgsql can translate .Date on a DateTimeOffset-aware column,
            // but for safety we pull (ServiceId, ViewedAt) and bucket in-memory.
            var rawViews = await _context.ListingViews
                .Where(lv => serviceIds.Contains(lv.ServiceId)
                          && lv.ViewedAt >= utcFrom
                          && lv.ViewedAt <  utcTo)
                .Select(lv => lv.ViewedAt)
                .ToListAsync();

            var dailyViews = rawViews
                .GroupBy(dt => DateOnly.FromDateTime(dt))
                .OrderBy(g => g.Key)
                .Select(g => new DailyViewBucketDto(g.Key, g.Count()))
                .ToList();

            // ── Listing metadata for the top-5 join ──────────────────────────
            var listingMeta = await _context.VendorServices
                .Where(vs => serviceIds.Contains(vs.ServiceId))
                .Select(vs => new { vs.ServiceId, vs.ServiceName, vs.CoverImageUrl })
                .ToListAsync();

            // ── Top 5 listings by view count in range ────────────────────────
            var topListings = listingMeta
                .Select(m => new ListingTrafficDto(
                    m.ServiceId,
                    m.ServiceName,
                    m.CoverImageUrl,
                    viewsPerListing.FirstOrDefault(v => v.ServiceId == m.ServiceId)?.Count ?? 0,
                    favoritesPerListing.FirstOrDefault(f => f.ServiceId == m.ServiceId)?.Count ?? 0))
                .OrderByDescending(l => l.ViewCount)
                .Take(5)
                .ToList();

            return new TrafficSummaryDto(
                TotalViews:     viewsPerListing.Sum(v => v.Count),
                TotalFavorites: favoritesPerListing.Sum(f => f.Count),
                DailyViews:     dailyViews,
                TopListings:    topListings
            );
        }

        // ─── 3. Favourited Listings ───────────────────────────────────────────

        public async Task<IReadOnlyList<FavoritedListingDto>> GetFavoritedListingsAsync(int vendorId)
        {
            var query = await _context.VendorServices
                .Where(vs => vs.VendorId == vendorId)
                .Select(vs => new
                {
                    vs.ServiceId,
                    vs.ServiceName,
                    vs.CoverImageUrl,
                    FavoriteCount = _context.CustomerFavorites.Count(cf => cf.ServiceId == vs.ServiceId)
                })
                .Where(x => x.FavoriteCount > 0)
                .OrderByDescending(x => x.FavoriteCount)
                .ToListAsync();

            return query.Select(x => new FavoritedListingDto(
                x.ServiceId,
                x.ServiceName,
                x.CoverImageUrl,
                x.FavoriteCount
            )).ToList();
        }

        // ─── 4. AI Suggestion Log ─────────────────────────────────────────────

        public async Task<AiSuggestionLogDto> GetAiSuggestionLogAsync(
            int vendorId, int? listingId = null)
        {
            // This table will be empty until the AI workflow feature ships.
            // We return a clean empty result, never an error.
            var serviceIds = await _context.VendorServices
                .Where(vs => vs.VendorId == vendorId)
                .Select(vs => vs.ServiceId)
                .ToListAsync();

            if (serviceIds.Count == 0)
            {
                return new AiSuggestionLogDto(0, Array.Empty<AiSuggestionListingGroupDto>());
            }

            // Apply optional single-listing filter.
            var query = _context.AiSuggestionLogs
                .Where(al => serviceIds.Contains(al.ServiceId));

            if (listingId.HasValue)
                query = query.Where(al => al.ServiceId == listingId.Value);

            // Pull all matching rows plus the listing name in one query.
            var rows = await query
                .Join(_context.VendorServices,
                    al => al.ServiceId,
                    vs => vs.ServiceId,
                    (al, vs) => new
                    {
                        al.SuggestionId,
                        al.ServiceId,
                        vs.ServiceName,
                        al.CustomerId,
                        al.Reasoning,
                        al.SuggestedAt
                    })
                .OrderByDescending(r => r.SuggestedAt)
                .ToListAsync();

            // Group in-memory (avoids complex GroupBy translation issues).
            var byListing = rows
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new AiSuggestionListingGroupDto(
                    g.Key.ServiceId,
                    g.Key.ServiceName,
                    g.Count(),
                    g.Select(r => new AiSuggestionEntryDto(
                        r.SuggestionId,
                        r.ServiceId,
                        r.ServiceName,
                        r.CustomerId,
                        r.Reasoning,
                        r.SuggestedAt))
                    .ToList()))
                .OrderByDescending(g => g.SuggestionCount)
                .ToList();

            return new AiSuggestionLogDto(
                TotalSuggestions: rows.Count,
                ByListing: byListing
            );
        }
    }
}
