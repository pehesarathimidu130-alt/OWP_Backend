namespace Backend.DTOs
{
    // =========================================================
    // Listing Review DTOs
    // Used by ListingReviewController (api/admin/listing-reviews)
    // =========================================================

    /// <summary>
    /// Lightweight row item used in the admin listing table.
    /// Maps from VendorService + Vendor + Category.
    /// </summary>
    public class ListingSummaryDto
    {
        public int ListingId      { get; set; }   // VendorService.ServiceId
        public int VendorId       { get; set; }
        public string VendorName  { get; set; } = string.Empty;   // Vendor.BusinessName
        public string Category    { get; set; } = string.Empty;   // Category.CategoryName
        public string? Location   { get; set; }                   // Vendor.City or Vendor.Address
        public decimal? Price     { get; set; }
        public bool IsPriceOnRequest { get; set; }
        public string? PriceDisplay  { get; set; }  // formatted price or "Price on Request"
        public double?  Rating    { get; set; }     // placeholder — extend when reviews exist
        public string ListedDate  { get; set; } = string.Empty;  // VendorService.CreatedAt
        public string Status      { get; set; } = string.Empty;  // VendorService.Status
        public string? CoverImageUrl { get; set; }
        public string? Initials   { get; set; }  // first two chars of VendorName for Avatar
    }

    /// <summary>
    /// Full detail view for a single listing — shown in the slide-out review modal.
    /// </summary>
    public class ListingDetailDto : ListingSummaryDto
    {
        // Vendor contact & profile info
        public string? VendorEmail  { get; set; }
        public string? VendorPhone  { get; set; }
        public string? Address      { get; set; }
        public string? City         { get; set; }
        public string? State        { get; set; }
        public string? Description  { get; set; }
        public string? ServiceName  { get; set; }
        public string? ServiceDescription { get; set; }
        public bool IsVendorApproved { get; set; }

        // Gallery images from VendorServiceImages
        public List<string> GalleryImages { get; set; } = new();

        // Category-specific attributes (null if not applicable)
        public CategoryDetailsDto? CategoryDetails { get; set; }
    }

    /// <summary>
    /// Flattened category-specific details for the review modal.
    /// Only the relevant fields are populated depending on the listing's category.
    /// </summary>
    public class CategoryDetailsDto
    {
        // --- Hotel / Venue ---
        public string? VenueType          { get; set; }
        public string? VenueSetting       { get; set; }
        public string? IndoorOutdoor      { get; set; }
        public int?    MinimumGuestCount  { get; set; }
        public string? CancellationPolicy { get; set; }

        // --- Photography ---
        public string? PhotographyStyle  { get; set; }
        public int?    PackageHours      { get; set; }

        // --- Music ---
        public string? MusicGenres        { get; set; }
        public string? PerformanceType    { get; set; }

        // --- Decorations ---
        public string? DecorationStyles   { get; set; }
        public string? SetupTime          { get; set; }

        // --- Catering ---
        public string? CuisineType        { get; set; }
        public int?    GuestCapacity      { get; set; }
    }

    /// <summary>
    /// Payload for approving or rejecting a listing.
    /// </summary>
    public class UpdateListingStatusDto
    {
        /// <summary>
        /// Target status: "Active" (approve) or "Inactive" (reject).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional reason shown to the vendor when rejected.
        /// </summary>
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// Listing counts returned by the /metrics endpoint.
    /// </summary>
    public class ListingMetricsDto
    {
        public int Total    { get; set; }
        public int Active   { get; set; }
        public int Pending  { get; set; }
        public int Inactive { get; set; }
        public int Draft    { get; set; }
    }
}
