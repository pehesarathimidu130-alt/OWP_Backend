using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("ListingViews")]
    public class ListingView
    {
        [Key]
        public long ViewId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public VendorService? VendorService { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime ViewedAt { get; set; }

        [MaxLength(30)]
        public string? Source { get; set; }
    }
}
