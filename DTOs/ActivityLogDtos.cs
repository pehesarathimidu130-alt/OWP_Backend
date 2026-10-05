using System;

namespace Backend.DTOs
{
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string TargetEntityType { get; set; } = string.Empty;
        public string? TargetEntityId { get; set; }
        public string? Details { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? ActingAdminId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorEmail { get; set; }
        public string? ActorRole { get; set; }
    }
}
