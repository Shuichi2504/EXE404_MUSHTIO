using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("UserNotifications")]
    public class UserNotification
    {
        [Key]
        public Guid UserNotificationId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(100)]
        public string DeviceKey { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceName { get; set; }

        [Required, MaxLength(80)]
        public string AlertType { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string MetricType { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string Severity { get; set; } = string.Empty;

        [Required, MaxLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        public double? Value { get; set; }

        public double? Threshold { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public AppUser? User { get; set; }
    }
}
