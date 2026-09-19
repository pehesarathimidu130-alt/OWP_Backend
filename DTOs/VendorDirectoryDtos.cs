namespace Backend.DTOs
{
    public class VendorSummaryDto
    {
        public string Id { get; set; } = string.Empty; // e.g. "#45"
        public int VendorId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Status { get; set; } = "Pending";
        public string? BusinessAddress { get; set; }
        public string? City { get; set; }
        public string? TaxId { get; set; }
        public int? RegistrationYear { get; set; }
        public int? YearsInBusiness { get; set; }
        public bool BusinessLicenseVerified { get; set; }
        public string AppliedDate { get; set; } = string.Empty;
        public string? ApprovedDate { get; set; }
        public string? SuspendedDate { get; set; }
        public string? SuspendReason { get; set; }
        public string? BanDate { get; set; }
        public string? BanReason { get; set; }
        public string? RejectReason { get; set; }
        public int ListingsCount { get; set; }
        public double? Rating { get; set; }
        public decimal Revenue { get; set; }
        public string? Description { get; set; }
        public string? WhyApplied { get; set; }
        public List<VendorDocDto> VerificationDocs { get; set; } = new();
    }

    public class VendorDocDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "pdf";
        public string? Url { get; set; }
    }

    public class UpdateVendorStatusDto
    {
        public string Status { get; set; } = string.Empty; // Approved, Rejected, Suspended, Banned, Pending
        public string? Reason { get; set; }
    }

    public class SaveVendorDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Category { get; set; }
        public string? BusinessAddress { get; set; }
        public string? City { get; set; }
        public string? TaxId { get; set; }
        public int? YearsInBusiness { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
    }
}
