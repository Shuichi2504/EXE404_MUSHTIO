namespace IoTAgriculture.DTOs.Auth
{
    public class AccountSummaryDto
    {
        public UserProfileDto Profile { get; set; } = new();
        public int ActiveSessionCount { get; set; }
        public int AssignedDeviceCount { get; set; }
        public List<string> Permissions { get; set; } = [];
    }
}
