using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class SubmitVendorRatingDto
    {
        public int? VendorId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "RatingValue must be an integer between 1 and 5.")]
        public int RatingValue { get; set; }
    }

    public class VendorRatingResponseDto
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public int CustomerId { get; set; }
        public int RatingValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsUpdated { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VendorRatingSummaryDto
    {
        public int VendorId { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
    }

    public class CustomerVendorRatingDto
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public int CustomerId { get; set; }
        public int RatingValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
