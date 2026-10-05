using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class FlaggedContent
    {
        [Key]
        public int Id { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public string ContentTitle { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Severity { get; set; } = "Medium";

        public string Status { get; set; } = "Open";

        public string ReportedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
