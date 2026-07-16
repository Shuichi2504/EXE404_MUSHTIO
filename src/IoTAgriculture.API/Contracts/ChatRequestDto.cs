using Microsoft.AspNetCore.Http;

namespace IoTAgriculture.API.Contracts;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public IFormFile? Image { get; set; }
}
