using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTAgriculture.Models
{
    [Table("Devices")]
    public class Device
    {
        [Key]
        public Guid DeviceId { get; set; }

        [Required, MaxLength(255)]
        public string DeviceName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MacAddress { get; set; }

        [MaxLength(100)]
        public string DeviceKey { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(255)]
        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastConnected { get; set; }
    }
}
