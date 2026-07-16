using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("UserDevices")]
    public class UserDevice
    {
        [Key]
        public Guid UserDeviceId { get; set; }

        public Guid UserId { get; set; }

        [Required, MaxLength(100)]
        public string DeviceKey { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceName { get; set; }

        public DateTime AssignedAt { get; set; }

        public AppUser? User { get; set; }
    }
}
