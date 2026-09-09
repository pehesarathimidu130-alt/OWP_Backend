using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("Admins")]
    public class Admin : BaseAuditableEntity
    {
        [Key]
        public int AdminId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(4)]
        public string SecurePin { get; set; } = string.Empty; // 4-digit PIN for admin verification

        [MaxLength(50)]
        public string AccessLevel { get; set; } = "Admin"; // SuperAdmin or Admin
    }
}