using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("UserSessions")]
    public class UserSession
    {
        [Key]
        public Guid SessionId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public AppUser? User { get; set; }
    }
}
