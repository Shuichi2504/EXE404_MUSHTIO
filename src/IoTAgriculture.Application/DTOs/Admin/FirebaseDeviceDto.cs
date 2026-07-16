namespace IoTAgriculture.DTOs.Admin
{
    public class FirebaseDeviceDto
    {
        public string DeviceKey { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? LastSeenAt { get; set; }
    }
}
