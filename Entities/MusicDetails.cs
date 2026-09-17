using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("MusicDetails")]
    public class MusicDetails : BaseAuditableEntity
    {
        [Key]
        public int ServiceId { get; set; }
        public VendorService? VendorService { get; set; }

        public string? PerformanceType { get; set; }
        public string? LineupSize { get; set; }
        public string? SetDuration { get; set; }
        public string[]? Genres { get; set; }
        
        public bool SoundSystemIncluded { get; set; } = false;
        public string? SoundSystemCapacity { get; set; }
        public string? WirelessMics { get; set; }
        
        public bool StageLightingIncluded { get; set; } = false;
        public string? LightingRig { get; set; }
        
        public string? SetupTimeRequired { get; set; }
        public bool? BackupHardwareOnSite { get; set; }
        
        public bool? McServicesIncluded { get; set; }
        public bool? BreakMusicIncluded { get; set; }
        public string? CustomSongsAllowed { get; set; }
        public string? OvertimeRate { get; set; }
    }
}
