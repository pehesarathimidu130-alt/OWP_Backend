using Backend.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CustomerManagementResponseDto
    {
        public int Id { get; set; }
        public string CoupleNames { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string WeddingDate { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int InquiriesCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string JoinDate { get; set; } = string.Empty;
        public string AvatarInitials { get; set; } = string.Empty;
    }

    public class CustomerStatusUpdateRequestDto
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "Active" or "Inactive"
    }
}
