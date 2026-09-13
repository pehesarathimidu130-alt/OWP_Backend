using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class VendorServiceRequestDto
    {
        [Required, MaxLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, 999999999)]
        public decimal? Price { get; set; }
    }

    public class VendorServiceResponseDto : VendorServiceRequestDto
    {
        public int ServiceId { get; set; }
    }

    public class VendorPerformanceRequestDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(2000)]
        public string? CustomerFeedback { get; set; }

        public DateTime? EventDate { get; set; }
    }

    public class VendorPerformanceResponseDto : VendorPerformanceRequestDto
    {
        public int PerformanceId { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class NotificationResponseDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
