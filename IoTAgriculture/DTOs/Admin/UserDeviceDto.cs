namespace IoTAgriculture.DTOs.Admin
{
    public class UserDeviceDto
    {
        public Guid UserDeviceId { get; set; }
        public Guid UserId { get; set; }
        public string DeviceKey { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
