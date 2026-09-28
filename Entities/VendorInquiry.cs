using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("VendorInquiries")]
    public class VendorInquiry : BaseAuditableEntity
    {
        [Key]
        public int InquiryId { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int? VendorId { get; set; }

        [ForeignKey("VendorId")]
        public Vendor? Vendor { get; set; }

        public int? ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public VendorService? VendorService { get; set; }

        public DateTime? WeddingDate { get; set; }

        public int? GuestCount { get; set; }

        public decimal? Budget { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
