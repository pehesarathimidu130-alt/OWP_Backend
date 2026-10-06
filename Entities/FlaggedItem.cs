using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    /// <summary>
    /// Represents a report/flag submitted by a customer against a specific vendor listing.
    /// This is the normalised entity with full relational data (listingId, reporterUserId, vendorId).
    /// </summary>
    public class FlaggedItem : BaseAuditableEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The VendorService (listing) that was flagged.</summary>
        public int ListingId { get; set; }

        /// <summary>The User who submitted the report.</summary>
        public int ReporterUserId { get; set; }

        /// <summary>The Vendor who owns the flagged listing.</summary>
        public int VendorId { get; set; }

        /// <summary>Content type: "Listing", "Review", "VendorProfile", etc.</summary>
        [MaxLength(50)]
        public string ContentType { get; set; } = "Listing";

        /// <summary>Human-readable title of the flagged content (denormalised for display).</summary>
        [MaxLength(200)]
        public string ContentTitle { get; set; } = string.Empty;

        /// <summary>The flag reason chosen from the predefined list.</summary>
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Severity level: Low | Medium | High.</summary>
        [MaxLength(20)]
        public string Severity { get; set; } = "Medium";

        /// <summary>Optional additional comments from the reporter.</summary>
        [MaxLength(1000)]
        public string? Comments { get; set; }

        /// <summary>Workflow status: Open | UnderReview | Dismissed | ContentRemoved.</summary>
        [MaxLength(30)]
        public string Status { get; set; } = "Open";

        /// <summary>Admin resolution note set when the flag is dismissed or content removed.</summary>
        [MaxLength(500)]
        public string? ResolutionNote { get; set; }

        /// <summary>Timestamp when an admin last reviewed/updated this flag.</summary>
        public DateTime? ReviewedAt { get; set; }

        // ── Navigation Properties ─────────────────────────────────────────────
        [ForeignKey(nameof(ListingId))]
        public virtual VendorService? Listing { get; set; }

        // Reporter FK references User.UserId
        public virtual User? Reporter { get; set; }

        // Vendor FK references Vendor.VendorId
        public virtual Vendor? Vendor { get; set; }
    }
}
