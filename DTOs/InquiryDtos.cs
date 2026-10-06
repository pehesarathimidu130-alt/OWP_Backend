using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class VendorInquiryResponseDto
    {
        public int InquiryId { get; set; }
        public int? VendorId { get; set; }
        public string? VendorName { get; set; }
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceImage { get; set; }
        public string? CategoryName { get; set; }

        public int? CustomerId { get; set; }
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerAvatar { get; set; }

        public DateTime? WeddingDate { get; set; }
        public int? GuestCount { get; set; }
        public decimal? Budget { get; set; }
        public string? Message { get; set; }
        public string? AttachmentUrl { get; set; }

        public string Status { get; set; } = "Pending";
        public string? VendorReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ReplyInquiryRequestDto
    {
        [Required(ErrorMessage = "Reply message cannot be empty.")]
        [MaxLength(2000, ErrorMessage = "Reply message cannot exceed 2000 characters.")]
        public string? ReplyMessage { get; set; }

        public string? Message { get; set; }
        public string? VendorResponse { get; set; }

        public string GetEffectiveMessage()
        {
            if (!string.IsNullOrWhiteSpace(ReplyMessage)) return ReplyMessage.Trim();
            if (!string.IsNullOrWhiteSpace(Message)) return Message.Trim();
            if (!string.IsNullOrWhiteSpace(VendorResponse)) return VendorResponse.Trim();
            return string.Empty;
        }
    }
}
