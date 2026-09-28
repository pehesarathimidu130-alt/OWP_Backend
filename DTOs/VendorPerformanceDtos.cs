namespace Backend.DTOs
{
    // ─── Active Listings Summary ───────────────────────────────────────────────

    public record ListingStatusCountDto(string Status, int Count);

    public record ActiveListingsSummaryDto(
        int TotalListings,
        IReadOnlyList<ListingStatusCountDto> ByStatus
    );

    // ─── Traffic Summary ───────────────────────────────────────────────────────

    public record DailyViewBucketDto(DateOnly Date, int Views);

    public record ListingTrafficDto(
        int ServiceId,
        string ServiceName,
        string? CoverImageUrl,
        int ViewCount,
        int FavoriteCount
    );

    public record TrafficSummaryDto(
        int TotalViews,
        int TotalFavorites,
        IReadOnlyList<DailyViewBucketDto> DailyViews,
        IReadOnlyList<ListingTrafficDto> TopListings
    );

    // ─── Favorited Listings ────────────────────────────────────────────────────

    public record FavoritedListingDto(
        int ServiceId,
        string ServiceName,
        string? CoverImageUrl,
        int FavoriteCount
    );

    // ─── AI Suggestion Log ─────────────────────────────────────────────────────

    public record AiSuggestionEntryDto(
        long SuggestionId,
        int ServiceId,
        string ServiceName,
        int CustomerId,
        string Reasoning,
        DateTime SuggestedAt
    );

    public record AiSuggestionListingGroupDto(
        int ServiceId,
        string ServiceName,
        int SuggestionCount,
        IReadOnlyList<AiSuggestionEntryDto> Entries
    );

    public record AiSuggestionLogDto(
        int TotalSuggestions,
        IReadOnlyList<AiSuggestionListingGroupDto> ByListing
    );

    // ─── Customer Recording DTOs ───────────────────────────────────────────────

    public record RecordViewRequestDto(string? Source = null);

    public record RecordViewResponseDto(
        bool Success,
        int ListingId,
        DateTime ViewedAt,
        bool IsAnonymous,
        string Message
    );

    public record ToggleFavoriteResponseDto(
        bool IsFavorite,
        int ListingId,
        string Message
    );
}
