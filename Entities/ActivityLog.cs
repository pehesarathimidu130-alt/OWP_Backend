using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int? ActingAdminId { get; set; }

        [ForeignKey("ActingAdminId")]
        public virtual Admin? ActingAdmin { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TargetEntityType { get; set; } = string.Empty; // "Admin", "Vendor", "Customer", "Auth"

        [MaxLength(100)]
        public string? TargetEntityId { get; set; }

        public string? Details { get; set; } // JSON or text: old→new values, reason if applicable

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
