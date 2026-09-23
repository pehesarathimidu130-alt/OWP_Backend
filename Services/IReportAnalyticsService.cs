using Backend.DTOs;

namespace Backend.Services
{
    /// <summary>
    /// Provides read-only aggregation queries over the Vendors table
    /// for the Directory Report Analytics admin page.
    /// All methods are query-only — no writes.
    /// </summary>
    public interface IReportAnalyticsService
    {
        /// <summary>
        /// Returns headline KPIs: total vendors, approval rate, average days to
        /// decision, top category, approval funnel, and category breakdown.
        /// </summary>
        Task<ReportAnalyticsSummaryDto> GetSummaryAsync();

        /// <summary>
        /// Returns one entry per calendar month for the last <paramref name="months"/>
        /// months, with total applications, approved count, and rejected count.
        /// </summary>
        Task<List<MonthlyApplicationDto>> GetMonthlyApplicationsAsync(int months = 6);

        /// <summary>
        /// Returns ban and suspension reasons normalised into labelled buckets,
        /// ordered by count descending.
        /// </summary>
        Task<List<BanSuspensionReasonDto>> GetBanSuspensionReasonsAsync();
    }
}
