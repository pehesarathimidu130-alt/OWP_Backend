using Backend.DTOs;

namespace Backend.Services
{
    public interface IVendorPerformanceService
    {
        /// <summary>
        /// Resolves the VendorId associated with the given user account ID.
        /// </summary>
        Task<int> GetVendorIdAsync(int userId);

        /// <summary>
        /// Returns counts of this vendor's listings grouped by status
        /// (Draft, Pending, Active, Inactive only).
        /// </summary>
        Task<ActiveListingsSummaryDto> GetActiveListingsSummaryAsync(int vendorId);

        /// <summary>
        /// Returns per-listing and vendor-total view/favorite counts, a
        /// daily-bucketed view series, and the top 5 listings by view count —
        /// all scoped to the given date range.
        /// </summary>
        Task<TrafficSummaryDto> GetTrafficSummaryAsync(int vendorId, DateTime from, DateTime to);

        /// <summary>
        /// Returns each listing that has at least one favourite, with its
        /// favourite count.
        /// </summary>
        Task<IReadOnlyList<FavoritedListingDto>> GetFavoritedListingsAsync(int vendorId);

        /// <summary>
        /// Returns AI suggestion log entries per listing, optionally filtered
        /// to a single listing. Returns an empty result (not an error) when no
        /// rows exist.
        /// </summary>
        Task<AiSuggestionLogDto> GetAiSuggestionLogAsync(int vendorId, int? listingId = null);
    }
}
