using IoTAgriculture.Data;
using IoTAgriculture.Models;
using Microsoft.EntityFrameworkCore;

namespace IoTAgriculture.Services;

public class ChatService
{
    private readonly IoTDbContext _db;

    public ChatService(IoTDbContext db)
    {
        _db = db;
    }

    public async Task SaveMessageAsync(
        Guid userId,
        string sender,
        string text,
        string? imageUrl = null)
    {
        var message = new ChatMessage
        {
            MessageId = Guid.NewGuid(),
            UserId = userId,
            Sender = sender,
            MessageText = text,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);

        await _db.SaveChangesAsync();

        await CleanupOldMessages(userId);
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(
        Guid userId,
        int limit = 20)
    {
        return await _db.ChatMessages
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ClearHistoryAsync(Guid userId)
    {
        var messages = await _db.ChatMessages
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        _db.ChatMessages.RemoveRange(messages);
        await _db.SaveChangesAsync();
    }

    private async Task CleanupOldMessages(Guid userId)
    {
        const int MAX_MESSAGES = 100;

        var count = await _db.ChatMessages
            .CountAsync(x => x.UserId == userId);

        if (count <= MAX_MESSAGES)
            return;

        var removeCount = count - MAX_MESSAGES;

        var oldMessages = await _db.ChatMessages
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAt)
            .Take(removeCount)
            .ToListAsync();

        _db.ChatMessages.RemoveRange(oldMessages);

        await _db.SaveChangesAsync();
    }
}
