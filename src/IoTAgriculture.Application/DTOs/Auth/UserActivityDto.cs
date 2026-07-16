namespace IoTAgriculture.DTOs.Auth
{
    public class UserActivityDto
    {
        public Guid UserActivityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "info";
        public DateTime CreatedAt { get; set; }
        public string CreatedLocal { get; set; } = string.Empty;
    }
}
