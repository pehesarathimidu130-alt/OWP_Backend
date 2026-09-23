namespace Backend.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Generic analytics response — returned by all three /api/analytics/* endpoints.
    // The frontend reads the same top-level keys regardless of which tab is active.
    // Fields that are not applicable for a given tab are left null / empty.
    // ─────────────────────────────────────────────────────────────────────────

    public class AnalyticsResponseDto
    {
        // ── Headline KPIs (stat cards) ────────────────────────────────────
        public int    TotalCount        { get; set; }   // total vendors / customers / admins
        public int    ActiveCount        { get; set; }   // approved vendors / active customers / active admins
        public int    PendingCount       { get; set; }   // pending vendors / inactive customers / pending actions
        public int?   ApprovalRatePercent{ get; set; }   // vendor tab only
        public double? AvgDaysToDecision  { get; set; }   // vendor tab only
        public string? TopCategory        { get; set; }   // vendor tab: top category name
        public int    NewThisMonth        { get; set; }   // registrations in the current calendar month
        public int    SuperAdminCount     { get; set; }   // admin tab only

        // ── Vendor-specific extras ─────────────────────────────────────────
        public int    TotalVendors         { get; set; }
        public int    ApprovedVendors      { get; set; }
        public int    RejectedVendors      { get; set; }
        public int    SuspendedVendors     { get; set; }
        public int    BannedVendors        { get; set; }

        // ── Customer-specific extras ───────────────────────────────────────
        public int    TotalCustomers       { get; set; }
        public int    ActiveCustomers      { get; set; }

        // ── Admin-specific extras ──────────────────────────────────────────
        public int    TotalAdmins          { get; set; }
        public int    ActiveAdmins         { get; set; }
        public int    PendingActions       { get; set; }  // placeholder — no Actions table yet

        // ── Chart data ────────────────────────────────────────────────────
        /// <summary>Last N months of registrations/applications for the monthly trend chart.</summary>
        public List<AnalyticsMonthlyDto>   MonthlyApplications   { get; set; } = new();

        /// <summary>Approval funnel or status breakdown for the bar charts.</summary>
        public List<AnalyticsLabelCountDto> Funnel               { get; set; } = new();

        /// <summary>Vendor category distribution (vendor tab only).</summary>
        public List<AnalyticsLabelCountDto> CategoryBreakdown    { get; set; } = new();

        /// <summary>Normalised ban/suspension reasons (vendor tab only).</summary>
        public List<AnalyticsBanReasonDto>  BanSuspensionReasons { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // One entry per calendar month — mirrors MonthlyApplicationDto in
    // ReportAnalyticsDtos so the same MonthlyTrendChart component works for both.
    // ─────────────────────────────────────────────────────────────────────────
    public class AnalyticsMonthlyDto
    {
        public string Month        { get; set; } = string.Empty;  // e.g. "Jan"
        public int    Year         { get; set; }
        public int    Applications { get; set; }  // total created in this month
        public int    Approved     { get; set; }  // status = Approved/Active in this month
        public int    Rejected     { get; set; }  // status = Rejected in this month
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Generic label + count — used for funnel, category breakdown, role counts.
    // ─────────────────────────────────────────────────────────────────────────
    public class AnalyticsLabelCountDto
    {
        public string Label { get; set; } = string.Empty;
        public int    Count { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ban / suspension reason bucket — mirrors BanSuspensionReasonDto in
    // ReportAnalyticsDtos so both controllers can share the same frontend chart.
    // ─────────────────────────────────────────────────────────────────────────
    public class AnalyticsBanReasonDto
    {
        public string Reason { get; set; } = string.Empty;
        public int    Count  { get; set; }
    }
}
