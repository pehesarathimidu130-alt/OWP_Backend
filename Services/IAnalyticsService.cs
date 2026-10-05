using Backend.DTOs;

namespace Backend.Services
{
    /// <summary>
    /// Provides live aggregation data for the three-tab Analytics dashboard
    /// (Vendors / Customers / Admins).  All methods are read-only.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Vendor tab — approval funnel, category distribution, monthly trend,
        /// ban/suspension reason breakdown and headline KPIs.
        /// </summary>
        Task<AnalyticsResponseDto> GetVendorAnalyticsAsync(int months = 6);

        /// <summary>
        /// Customer tab — total, active/inactive ratio, new-this-month,
        /// monthly registration trend.
        /// </summary>
        Task<AnalyticsResponseDto> GetCustomerAnalyticsAsync(int months = 6);

        /// <summary>
        /// Admin tab — total admins, super-admin count, active count,
        /// monthly join trend.
        /// </summary>
        Task<AnalyticsResponseDto> GetAdminAnalyticsAsync(int months = 6);
    }
}
