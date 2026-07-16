using System.ComponentModel.DataAnnotations;

namespace IoTAgriculture.DTOs.Admin
{
    public class AssignDeviceRequestDto
    {
        [Required, MaxLength(100)]
        public string DeviceKey { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceName { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }
    }
}
