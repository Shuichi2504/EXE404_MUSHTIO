using IoTAgriculture.DTOs;
using IoTAgriculture.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTAgriculture.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly GeminiService _geminiService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        GeminiService geminiService,
        ILogger<AiController> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    [HttpPost("chat")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Chat(
        [FromForm] ChatRequestDto request,
        [FromServices] ChatService chatService)
    {
        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new { message = "UserId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Message) &&
            (request.Image == null || request.Image.Length == 0))
        {
            return BadRequest(new { message = "Message or image is required" });
        }

        var message = string.IsNullOrWhiteSpace(request.Message)
            ? "Hay xem hinh anh nay va dua ra nhan xet."
            : request.Message.Trim();

        await SaveMessageSafelyAsync(
            chatService,
            request.UserId,
            "user",
            message,
            request.Image?.FileName);

        string answer;
        try
        {
            answer = await _geminiService.AskAsync(
                message,
                request.Image,
                request.FarmContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI chat request failed");
            var userMessage = ex is InvalidOperationException &&
                ex.Message.Contains("API key", StringComparison.OrdinalIgnoreCase)
                ? "Backend chua cau hinh Gemini API key. Hay them Gemini__ApiKey trong Azure App Service."
                : "AI khong phan hoi duoc. Vui long kiem tra Gemini API key, model hoac ket noi mang cua backend.";

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = userMessage });
        }

        await SaveMessageSafelyAsync(
            chatService,
            request.UserId,
            "ai",
            answer);

        return Ok(new ChatResponseDto { Answer = answer });
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> History(
        Guid userId,
        [FromServices] ChatService chatService)
    {
        var messages = await chatService.GetHistoryAsync(userId);
        return Ok(messages);
    }

    [HttpDelete("history/{userId}")]
    public async Task<IActionResult> ClearHistory(
        Guid userId,
        [FromServices] ChatService chatService)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "UserId is required" });
        }

        await chatService.ClearHistoryAsync(userId);
        return NoContent();
    }

    private async Task SaveMessageSafelyAsync(
        ChatService chatService,
        Guid userId,
        string sender,
        string text,
        string? imageUrl = null)
    {
        try
        {
            await chatService.SaveMessageAsync(userId, sender, text, imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save AI chat message");
        }
    }
}
