using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("CustomerNotificationPreferences")]
    public class CustomerNotificationPreferences : BaseAuditableEntity
    {
        [Key]
        public int PreferenceId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public bool InquiryUpdates { get; set; } = true;

        public bool PriceChanges { get; set; } = true;
    }
}
