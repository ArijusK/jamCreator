using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamCreator.Data;
using JamCreator.Shared.Models.DTOs;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChatController(AppDbContext db) => _db = db;

    // GET api/chat/history?take=50 (most recent first)
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetHistory([FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 200);

        var items = await _db.ChatMessages
            .AsNoTracking()
            .OrderByDescending(m => m.SentAtUtc)
            .Take(take)
            .Select(m => new ChatMessageDto
            {
                User = m.User,
                Text = m.Text,
                Avatar = m.Avatar,
                SentAtUtc = m.SentAtUtc
            })
            .ToListAsync();

        // return ascending order for UI
        items.Reverse();
        return Ok(items);
    }
}
