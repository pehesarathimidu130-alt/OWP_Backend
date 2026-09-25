using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("AiSuggestionLogs")]
    public class AiSuggestionLog
    {
        [Key]
        public long SuggestionId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public VendorService? VendorService { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reasoning { get; set; } = string.Empty;

        public DateTime SuggestedAt { get; set; }
    }
}
