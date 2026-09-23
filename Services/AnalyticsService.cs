using Backend.Data;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    /// <summary>
    /// Implements <see cref="IAnalyticsService"/> by running AsNoTracking
    /// aggregation queries against AppDbContext.
    /// All three methods materialise only the minimal columns needed and
    /// perform grouping in C# so the queries work on any EF-supported database.
    ///
    /// IMPORTANT: Customer and Admin tables currently have very few columns
    /// (no Status / IsActive on Customer; no IsActive on Admin).  Where a
    /// direct DB column is missing the service falls back to the related
    /// User.IsActive flag or reasonable defaults so the frontend never gets
    /// an error response.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        // ── Known vendor categories (matches Category seed data) ──────────
        private static readonly string[] KnownCategories =
        {
            "Hotel / Venue", "Photography", "Music", "Decorations", "Catering",
        };

        // ── Ban / suspension reason keyword → bucket ──────────────────────
        private static readonly (string keyword, string label)[] ReasonBuckets =
        {
            ("fake",        "Fake or misleading documents"),
            ("document",    "Fake or misleading documents"),
            ("mislead",     "Fake or misleading documents"),
            ("complaint",   "Customer complaints"),
            ("fraud",       "Payment / fraud concerns"),
            ("payment",     "Payment / fraud concerns"),
            ("policy",      "Policy violation"),
            ("violation",   "Policy violation"),
            ("unresponsive","Unresponsive vendor"),
            ("inactive",    "Unresponsive vendor"),
        };
        private const string FallbackReason = "Other";

        public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger  = logger;
        }

        // ═════════════════════════════════════════════════════════════════════
        // VENDOR ANALYTICS
        // ═════════════════════════════════════════════════════════════════════
        public async Task<AnalyticsResponseDto> GetVendorAnalyticsAsync(int months = 6)
        {
            try
            {
                var vendors = await _context.Vendors
                    .AsNoTracking()
                    .Select(v => new { v.Status, v.Category, v.CreatedAt, v.UpdatedAt, v.Description })
                    .ToListAsync();

                // ── Status counts ──────────────────────────────────────────
                int Count(string s) =>
                    vendors.Count(v => v.Status.Equals(s, StringComparison.OrdinalIgnoreCase));

                var approved  = Count("Approved") + Count("Active");
                var rejected  = Count("Rejected");
                var pending   = Count("Pending");
                var suspended = Count("Suspended");
                var banned    = Count("Banned");
                var total     = vendors.Count;

                // ── Approval rate ──────────────────────────────────────────
                var decided = approved + rejected;
                int? approvalRate = decided > 0
                    ? (int)Math.Round(approved / (double)decided * 100)
                    : null;

                // ── Avg days to decision (UpdatedAt proxy) ─────────────────
                var decidedVendors = vendors
                    .Where(v => v.Status is "Approved" or "Active" or "Rejected" or "Suspended" or "Banned")
                    .ToList();

                double? avgDays = null;
                if (decidedVendors.Count > 0)
                {
                    var durations = decidedVendors
                        .Select(v => (v.UpdatedAt - v.CreatedAt).TotalDays)
                        .Where(d => d >= 0).ToList();
                    if (durations.Count > 0)
                        avgDays = Math.Round(durations.Average(), 1);
                }

                // ── Category breakdown ─────────────────────────────────────
                var categoryBreakdown = KnownCategories
                    .Select(cat => new AnalyticsLabelCountDto
                    {
                        Label = cat,
                        Count = vendors.Count(v =>
                            (v.Category ?? "").Equals(cat, StringComparison.OrdinalIgnoreCase)),
                    }).ToList();

                var otherCount = vendors.Count(v =>
                    !string.IsNullOrWhiteSpace(v.Category) &&
                    !KnownCategories.Any(k => k.Equals(v.Category, StringComparison.OrdinalIgnoreCase)));
                if (otherCount > 0)
                    categoryBreakdown.Add(new AnalyticsLabelCountDto { Label = "Other", Count = otherCount });

                var topCategory = categoryBreakdown.MaxBy(c => c.Count);

                // ── Monthly trend ──────────────────────────────────────────
                var monthly = BuildMonthlyTrend(
                    months,
                    vendors.Select(v => (v.CreatedAt, v.Status)).ToList(),
                    isApproved: s => s.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                                  || s.Equals("Active",   StringComparison.OrdinalIgnoreCase),
                    isRejected: s => s.Equals("Rejected", StringComparison.OrdinalIgnoreCase));

                // ── This month ─────────────────────────────────────────────
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var newThisMonth = vendors.Count(v => v.CreatedAt >= monthStart);

                // ── Ban / suspension reasons ───────────────────────────────
                var banReasons = ClassifyReasons(
                    vendors
                        .Where(v => v.Status is "Banned" or "Suspended")
                        .Select(v => v.Description)
                        .ToList());

                // ── Funnel ─────────────────────────────────────────────────
                var funnel = new List<AnalyticsLabelCountDto>
                {
                    new() { Label = "Pending",   Count = pending   },
                    new() { Label = "Approved",  Count = approved  },
                    new() { Label = "Rejected",  Count = rejected  },
                    new() { Label = "Suspended", Count = suspended },
                    new() { Label = "Banned",    Count = banned    },
                };

                return new AnalyticsResponseDto
                {
                    TotalCount           = total,
                    ActiveCount          = approved,
                    PendingCount         = pending,
                    ApprovalRatePercent  = approvalRate,
                    AvgDaysToDecision    = avgDays,
                    TopCategory          = topCategory?.Label,
                    NewThisMonth         = newThisMonth,
                    TotalVendors         = total,
                    ApprovedVendors      = approved,
                    RejectedVendors      = rejected,
                    SuspendedVendors     = suspended,
                    BannedVendors        = banned,
                    MonthlyApplications  = monthly,
                    Funnel               = funnel,
                    CategoryBreakdown    = categoryBreakdown,
                    BanSuspensionReasons = banReasons,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyticsService.GetVendorAnalyticsAsync failed");
                throw;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // CUSTOMER ANALYTICS
        // ═════════════════════════════════════════════════════════════════════
        public async Task<AnalyticsResponseDto> GetCustomerAnalyticsAsync(int months = 6)
        {
            try
            {
                // Customer entity has no Status/IsActive — use linked User.IsActive
                var customers = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Select(c => new { c.CreatedAt, IsActive = c.User != null && c.User.IsActive })
                    .ToListAsync();

                var total       = customers.Count;
                var active      = customers.Count(c => c.IsActive);
                var inactive    = total - active;
                var monthStart  = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var newThisMonth = customers.Count(c => c.CreatedAt >= monthStart);

                var monthly = BuildMonthlyTrend(
                    months,
                    customers.Select(c => (c.CreatedAt, c.IsActive ? "Active" : "Inactive")).ToList(),
                    isApproved: s => s == "Active",
                    isRejected: s => s == "Inactive");

                var funnel = new List<AnalyticsLabelCountDto>
                {
                    new() { Label = "Active",   Count = active   },
                    new() { Label = "Inactive", Count = inactive },
                };

                return new AnalyticsResponseDto
                {
                    TotalCount          = total,
                    ActiveCount         = active,
                    PendingCount        = inactive,
                    NewThisMonth        = newThisMonth,
                    TotalCustomers      = total,
                    ActiveCustomers     = active,
                    MonthlyApplications = monthly,
                    Funnel              = funnel,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyticsService.GetCustomerAnalyticsAsync failed");
                throw;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // ADMIN ANALYTICS
        // ═════════════════════════════════════════════════════════════════════
        public async Task<AnalyticsResponseDto> GetAdminAnalyticsAsync(int months = 6)
        {
            try
            {
                // Admin.AccessLevel = "Admin" | "SuperAdmin"
                // Active status via linked User.IsActive
                var admins = await _context.Admins
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Select(a => new
                    {
                        a.CreatedAt,
                        a.AccessLevel,
                        IsActive = a.User != null && a.User.IsActive,
                    })
                    .ToListAsync();

                var total       = admins.Count;
                var superAdmins = admins.Count(a =>
                    a.AccessLevel.Contains("Super", StringComparison.OrdinalIgnoreCase));
                var active      = admins.Count(a => a.IsActive);
                var monthStart  = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var newThisMonth = admins.Count(a => a.CreatedAt >= monthStart);

                var monthly = BuildMonthlyTrend(
                    months,
                    admins.Select(a => (a.CreatedAt, a.IsActive ? "Active" : "Inactive")).ToList(),
                    isApproved: s => s == "Active",
                    isRejected: s => s == "Inactive");

                var funnel = new List<AnalyticsLabelCountDto>
                {
                    new() { Label = "Admin",      Count = total - superAdmins },
                    new() { Label = "Super Admin", Count = superAdmins         },
                };

                return new AnalyticsResponseDto
                {
                    TotalCount      = total,
                    ActiveCount     = active,
                    PendingCount    = 0,          // no pending-actions table yet
                    SuperAdminCount = superAdmins,
                    NewThisMonth    = newThisMonth,
                    TotalAdmins     = total,
                    ActiveAdmins    = active,
                    PendingActions  = 0,
                    MonthlyApplications = monthly,
                    Funnel          = funnel,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyticsService.GetAdminAnalyticsAsync failed");
                throw;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Private helpers
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a rolling monthly trend list from a flat in-memory collection.
        /// Works for vendors, customers, and admins by accepting generic status predicates.
        /// </summary>
        private static List<AnalyticsMonthlyDto> BuildMonthlyTrend(
            int months,
            List<(DateTime CreatedAt, string Status)> rows,
            Func<string, bool> isApproved,
            Func<string, bool> isRejected)
        {
            var windowStart = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(months - 1));

            var result = new List<AnalyticsMonthlyDto>(months);
            for (int i = 0; i < months; i++)
            {
                var mStart = windowStart.AddMonths(i);
                var mEnd   = mStart.AddMonths(1);

                var inMonth = rows
                    .Where(r => r.CreatedAt >= mStart && r.CreatedAt < mEnd)
                    .ToList();

                result.Add(new AnalyticsMonthlyDto
                {
                    Month        = mStart.ToString("MMM"),
                    Year         = mStart.Year,
                    Applications = inMonth.Count,
                    Approved     = inMonth.Count(r => isApproved(r.Status)),
                    Rejected     = inMonth.Count(r => isRejected(r.Status)),
                });
            }
            return result;
        }

        /// <summary>Normalises a list of free-text reasons into labelled buckets.</summary>
        private static List<AnalyticsBanReasonDto> ClassifyReasons(List<string?> rawReasons)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in rawReasons)
            {
                var label = ClassifyOne(raw);
                counts.TryGetValue(label, out var n);
                counts[label] = n + 1;
            }
            return counts
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new AnalyticsBanReasonDto { Reason = kv.Key, Count = kv.Value })
                .ToList();
        }

        private static string ClassifyOne(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return FallbackReason;
            var lower = raw.ToLowerInvariant();
            foreach (var (kw, label) in ReasonBuckets)
                if (lower.Contains(kw)) return label;
            return FallbackReason;
        }
    }
}
