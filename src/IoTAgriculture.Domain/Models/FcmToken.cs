using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("FcmTokens")]
    public class FcmToken
    {
        [Key]
        public Guid FcmTokenId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        [MaxLength(64)]
        public string Platform { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public AppUser? User { get; set; }
    }
}
