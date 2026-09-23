namespace Backend.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Top-level response returned by GET /api/ReportAnalytics/summary
    // ─────────────────────────────────────────────────────────────────────────
    public class ReportAnalyticsSummaryDto
    {
        /// <summary>Total number of vendor profiles in the system.</summary>
        public int TotalVendors { get; set; }

        /// <summary>
        /// Approval rate as a percentage (0–100), computed as
        /// Approved / (Approved + Rejected) * 100.
        /// Null when no decision has been made yet.
        /// </summary>
        public int? ApprovalRatePercent { get; set; }

        /// <summary>
        /// Average calendar days between a vendor's CreatedAt and the date
        /// their status was last changed to Approved/Rejected/Banned/Suspended.
        /// Null when no completed decisions exist.
        /// </summary>
        public double? AvgDaysToDecision { get; set; }

        /// <summary>Category that has the most vendors registered under it.</summary>
        public string? TopCategory { get; set; }

        /// <summary>Breakdown of vendor counts per status.</summary>
        public List<StatusCountDto> Funnel { get; set; } = new();

        /// <summary>Breakdown of vendor counts per category.</summary>
        public List<CategoryCountDto> CategoryBreakdown { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/ReportAnalytics/monthly-applications
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Monthly aggregation of vendor applications for a rolling window.
    /// Each entry represents one calendar month.
    /// </summary>
    public class MonthlyApplicationDto
    {
        /// <summary>Short month label, e.g. "Jan", "Feb".</summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>Four-digit year, e.g. 2025.</summary>
        public int Year { get; set; }

        /// <summary>Total vendor profiles created in this month.</summary>
        public int Applications { get; set; }

        /// <summary>Vendors whose status is Approved, created in this month.</summary>
        public int Approved { get; set; }

        /// <summary>Vendors whose status is Rejected, created in this month.</summary>
        public int Rejected { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/ReportAnalytics/ban-suspension-reasons
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// A reason category and the number of vendors that fall under it.
    /// The backend normalises free-text ban/suspend reason fields into
    /// a small set of labelled buckets.
    /// </summary>
    public class BanSuspensionReasonDto
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────
    public class StatusCountDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CategoryCountDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
