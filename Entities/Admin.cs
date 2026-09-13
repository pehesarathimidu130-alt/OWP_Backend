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
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string Department { get; set; } = "Administration";

        [Required]
        [MaxLength(50)]
        public string AccessLevel { get; set; } = "Admin"; // "Admin" or "SuperAdmin"

        [Required]
        [MaxLength(255)]
        public string SecurePinHash { get; set; } = string.Empty; // BCrypt-hashed 4-digit PIN (No plaintext stored)
    }
}