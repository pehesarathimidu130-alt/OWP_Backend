using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("DecorationsDetails")]
    public class DecorationsDetails : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        public string[]? PrimaryStyles { get; set; }
        
        public bool ProvidesFlorals { get; set; } = false;
        public string[]? FloralTypes { get; set; }
        
        public string[]? AvailableSetups { get; set; }
        
        public string? TablewareLinens { get; set; }
        public bool? CustomSignageIncluded { get; set; }
        public bool? LoungePropsAvailable { get; set; }
        
        public string? SetupTimeRequired { get; set; }
        public bool? SameDayTeardownIncluded { get; set; }
        public string? VenueRestrictions { get; set; }
        
        public bool OutstationDecorAllowed { get; set; } = false;
        public string? TravelFeePolicy { get; set; }
        
        public bool? FreeConsultation { get; set; }
        public bool? CustomMoodboards { get; set; }
        public string? DesignFeePolicy { get; set; }
        public string? MinimumBudget { get; set; }
    }
}
