using System.Collections.Generic;

namespace Backend.DTOs
{
    // ─── Request DTOs ───────────────────────────────────────────────────────────

    /// <summary>Payload sent by the mobile app when a customer flags a listing.</summary>
    public class CreateFlagDto
    {
        public int ListingId { get; set; }
        public int ReporterUserId { get; set; }
        public int VendorId { get; set; }
        public string ContentType { get; set; } = "Listing";
        public string ContentTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string? Comments { get; set; }
    }

    /// <summary>Payload for updating a flag's admin status.</summary>
    public class UpdateFlagStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }

    // ─── Response DTOs ──────────────────────────────────────────────────────────

    /// <summary>Full flag details for admin view.</summary>
    public class FlaggedItemDto
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public int ReporterUserId { get; set; }
        public int VendorId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string ContentTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }

        // Denormalised display helpers
        public string ReporterName { get; set; } = string.Empty;
        public string ReporterEmail { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? ReviewedAt { get; set; }
    }

    /// <summary>Lightweight check response used by the mobile app to prevent duplicate flags.</summary>
    public class FlagCheckDto
    {
        public bool HasReported { get; set; }
        public int? ExistingFlagId { get; set; }
    }
}
