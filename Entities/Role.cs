using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("Roles")]
    public class Role : BaseAuditableEntity
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty; // ADMIN, SUPER_ADMIN, Vendor, Customer

        public static implicit operator Role(string roleName) => new Role { RoleName = roleName };
        public static implicit operator string(Role? role) => role?.RoleName ?? string.Empty;
    }
}