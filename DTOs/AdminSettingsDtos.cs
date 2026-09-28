using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class AdminProfileResponseDto
    {
        public int AdminId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AdminNotificationPreferencesDto
    {
        public bool NewVendorPending { get; set; } = true;
        public bool FlaggedContent { get; set; } = true;
        public bool CustomerComplaint { get; set; } = true;
        public bool AiWorkflowApproval { get; set; } = false;
        public bool WeeklySummary { get; set; } = true;
    }

    public class UpdateAdminNotificationPreferencesDto
    {
        public bool? NewVendorPending { get; set; }
        public bool? FlaggedContent { get; set; }
        public bool? CustomerComplaint { get; set; }
        public bool? AiWorkflowApproval { get; set; }
        public bool? WeeklySummary { get; set; }
    }
}
