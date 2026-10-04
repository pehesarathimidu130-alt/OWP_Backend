using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities
{
    [Table("Customers")]
    public class Customer : BaseAuditableEntity
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ProfilePhotoUrl { get; set; }

        [NotMapped]
        public bool IsActive
        {
            get => User?.IsActive ?? true;
            set
            {
                if (User != null)
                {
                    User.IsActive = value;
                }
            }
        }
    }
}