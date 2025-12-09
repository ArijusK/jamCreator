using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamCreator.Data;
using JamCreator.Shared.Models.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChatController(AppDbContext db) => _db = db;

    // GET: api/chat/{sessionId}?take=50
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetHistory(
        string sessionId,
        [FromQuery] int take = 50)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest("Missing sessionId");

        take = Math.Clamp(take, 1, 200);

        var items = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SentAtUtc)
            .Take(take)
            .Select(m => new ChatMessageDto
            {
                User      = m.User,
                Text      = m.Text,
                Avatar    = m.Avatar,
                SentAtUtc = m.SentAtUtc
            })
            .ToListAsync();

        items.Reverse();
        return Ok(items);
    }
}
