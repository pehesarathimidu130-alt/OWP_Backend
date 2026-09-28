using Backend.Data;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    /// <summary>
    /// Implements <see cref="IReportAnalyticsService"/> by running aggregation
    /// queries directly against <see cref="AppDbContext.Vendors"/>.
    /// All queries are AsNoTracking — this service never writes to the database.
    /// </summary>
    public class ReportAnalyticsService : IReportAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReportAnalyticsService> _logger;

        // ── Known category names (must match Category seed data in AppDbContext) ──
        private static readonly string[] KnownCategories =
        {
            "Hotel / Venue",
            "Photography",
            "Music",
            "Decorations",
            "Catering",
        };

        // ── Ban/suspension reason keywords → human-readable bucket label ──────
        // The Vendor entity stores free-text in Status-related fields that the
        // VendorDirectory endpoints write.  We pattern-match to normalise them.
        private static readonly (string keyword, string label)[] ReasonBuckets =
        {
            ("fake",       "Fake or misleading documents"),
            ("document",   "Fake or misleading documents"),
            ("mislead",    "Fake or misleading documents"),
            ("complaint",  "Customer complaints"),
            ("fraud",      "Payment / fraud concerns"),
            ("payment",    "Payment / fraud concerns"),
            ("policy",     "Policy violation"),
            ("violation",  "Policy violation"),
            ("unresponsive","Unresponsive vendor"),
            ("inactive",   "Unresponsive vendor"),
        };
        private const string FallbackReasonLabel = "Other";

        public ReportAnalyticsService(AppDbContext context, ILogger<ReportAnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetSummaryAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ReportAnalyticsSummaryDto> GetSummaryAsync()
        {
            try
            {
                // Pull only the columns we need — avoids loading full entity graph.
                var vendors = await _context.Vendors
                    .AsNoTracking()
                    .Select(v => new
                    {
                        v.Status,
                        v.Category,
                        v.CreatedAt,
                        v.UpdatedAt,   // used as a proxy for decision date
                    })
                    .ToListAsync();

                var total = vendors.Count;

                // ── Funnel counts ──────────────────────────────────────────
                int CountStatus(string s) =>
                    vendors.Count(v => v.Status.Equals(s, StringComparison.OrdinalIgnoreCase));

                var approved  = CountStatus("Approved") + CountStatus("Active");
                var rejected  = CountStatus("Rejected");
                var pending   = CountStatus("Pending");
                var suspended = CountStatus("Suspended");
                var banned    = CountStatus("Banned");

                var funnel = new List<StatusCountDto>
                {
                    new() { Label = "Pending",   Count = pending   },
                    new() { Label = "Approved",  Count = approved  },
                    new() { Label = "Rejected",  Count = rejected  },
                    new() { Label = "Suspended", Count = suspended },
                    new() { Label = "Banned",    Count = banned    },
                };

                // ── Approval rate ──────────────────────────────────────────
                var decided = approved + rejected;
                int? approvalRate = decided > 0
                    ? (int)Math.Round(approved / (double)decided * 100)
                    : null;

                // ── Average days to decision ───────────────────────────────
                // We use UpdatedAt as the best available proxy: for vendors whose
                // status was changed by admin actions, UpdatedAt reflects that moment.
                var decidedVendors = vendors
                    .Where(v => v.Status is "Approved" or "Active" or "Rejected"
                                         or "Suspended" or "Banned")
                    .ToList();

                double? avgDays = null;
                if (decidedVendors.Count > 0)
                {
                    var durations = decidedVendors
                        .Select(v => (v.UpdatedAt - v.CreatedAt).TotalDays)
                        .Where(d => d >= 0)
                        .ToList();

                    if (durations.Count > 0)
                        avgDays = Math.Round(durations.Average(), 1);
                }

                // ── Category breakdown ─────────────────────────────────────
                var categoryBreakdown = KnownCategories
                    .Select(cat => new CategoryCountDto
                    {
                        Label = cat,
                        Count = vendors.Count(v =>
                            (v.Category ?? string.Empty)
                                .Equals(cat, StringComparison.OrdinalIgnoreCase)),
                    })
                    .ToList();

                // Vendors with a category not in KnownCategories → "Other"
                var otherCount = vendors.Count(v =>
                    !string.IsNullOrWhiteSpace(v.Category) &&
                    !KnownCategories.Any(k =>
                        k.Equals(v.Category, StringComparison.OrdinalIgnoreCase)));

                if (otherCount > 0)
                    categoryBreakdown.Add(new CategoryCountDto { Label = "Other", Count = otherCount });

                var topCategory = categoryBreakdown
                    .OrderByDescending(c => c.Count)
                    .FirstOrDefault();

                return new ReportAnalyticsSummaryDto
                {
                    TotalVendors       = total,
                    ApprovalRatePercent = approvalRate,
                    AvgDaysToDecision  = avgDays,
                    TopCategory        = topCategory?.Label,
                    Funnel             = funnel,
                    CategoryBreakdown  = categoryBreakdown,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportAnalyticsService.GetSummaryAsync failed");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetMonthlyApplicationsAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<MonthlyApplicationDto>> GetMonthlyApplicationsAsync(int months = 6)
        {
            try
            {
                // Window start: first day of the month `months` months ago.
                var windowStart = new DateTime(
                    DateTime.UtcNow.Year,
                    DateTime.UtcNow.Month,
                    1,
                    0, 0, 0,
                    DateTimeKind.Utc)
                    .AddMonths(-(months - 1));

                var vendors = await _context.Vendors
                    .AsNoTracking()
                    .Where(v => v.CreatedAt >= windowStart)
                    .Select(v => new { v.CreatedAt, v.Status })
                    .ToListAsync();

                // Build result for each month in the window, in chronological order.
                var result = new List<MonthlyApplicationDto>(months);
                for (int i = 0; i < months; i++)
                {
                    var monthStart = windowStart.AddMonths(i);
                    var monthEnd   = monthStart.AddMonths(1);

                    var inMonth = vendors
                        .Where(v => v.CreatedAt >= monthStart && v.CreatedAt < monthEnd)
                        .ToList();

                    result.Add(new MonthlyApplicationDto
                    {
                        Month        = monthStart.ToString("MMM"),
                        Year         = monthStart.Year,
                        Applications = inMonth.Count,
                        Approved     = inMonth.Count(v =>
                            v.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                            v.Status.Equals("Active",   StringComparison.OrdinalIgnoreCase)),
                        Rejected     = inMonth.Count(v =>
                            v.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)),
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportAnalyticsService.GetMonthlyApplicationsAsync failed");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetBanSuspensionReasonsAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<BanSuspensionReasonDto>> GetBanSuspensionReasonsAsync()
        {
            try
            {
                // Pull the raw free-text reason fields for banned/suspended vendors.
                var reasons = await _context.Vendors
                    .AsNoTracking()
                    .Where(v => v.Status == "Banned" || v.Status == "Suspended")
                    .Select(v => v.Description)   // Description stores admin reason notes
                    .ToListAsync();

                // Normalise each raw string into a bucket label.
                var bucketCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in reasons)
                {
                    var label = ClassifyReason(raw);
                    bucketCounts.TryGetValue(label, out var current);
                    bucketCounts[label] = current + 1;
                }

                // If no banned/suspended vendors exist return empty list.
                if (bucketCounts.Count == 0)
                    return new List<BanSuspensionReasonDto>();

                return bucketCounts
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => new BanSuspensionReasonDto { Reason = kv.Key, Count = kv.Value })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReportAnalyticsService.GetBanSuspensionReasonsAsync failed");
                throw;
            }
        }

        // ── Helper: map a free-text reason string → bucket label ─────────────
        private static string ClassifyReason(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return FallbackReasonLabel;

            var lower = raw.ToLowerInvariant();
            foreach (var (keyword, label) in ReasonBuckets)
            {
                if (lower.Contains(keyword))
                    return label;
            }
            return FallbackReasonLabel;
        }
    }
}
