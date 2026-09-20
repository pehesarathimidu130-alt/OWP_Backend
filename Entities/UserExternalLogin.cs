using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Entities
{
    [Table("UserExternalLogins")]
    [Index(nameof(Provider), nameof(ProviderSubject), IsUnique = true)]
    public class UserExternalLogin : BaseAuditableEntity
    {
        [Key]
        public int UserExternalLoginId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string ProviderSubject { get; set; } = string.Empty;
    }
}
