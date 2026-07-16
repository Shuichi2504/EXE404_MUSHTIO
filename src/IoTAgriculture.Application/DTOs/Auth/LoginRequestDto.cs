using System.ComponentModel.DataAnnotations;

namespace IoTAgriculture.DTOs.Auth
{
    public class LoginRequestDto
    {
        [MaxLength(120)]
        public string Identifier { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
