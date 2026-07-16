namespace IoTAgriculture.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfileDto User { get; set; } = new();
    }
}
