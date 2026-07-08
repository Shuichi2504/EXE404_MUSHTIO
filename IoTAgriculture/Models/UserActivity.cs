using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("UserActivities")]
    public class UserActivity
    {
        [Key]
        public Guid UserActivityId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(80)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(64)]
        public string Severity { get; set; } = "info";

        public DateTime CreatedAt { get; set; }

        public AppUser? User { get; set; }
    }
}
