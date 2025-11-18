using Microsoft.AspNetCore.SignalR;
using JamCreator.Data;              // AppDbContext namespace
using JamCreator.Shared.Models;

public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    public ChatHub(AppDbContext db) => _db = db;

    public async Task SendMessage(string user, string message, string avatar)
    {
        var entity = new ChatMessage
        {
            User = string.IsNullOrWhiteSpace(user) ? "Guest" : user.Trim(),
            Text = message.Trim(),
            Avatar = string.IsNullOrWhiteSpace(avatar) ? null : avatar.Trim(),
            SentAtUtc = DateTime.UtcNow
        };

        _db.ChatMessages.Add(entity);
        await _db.SaveChangesAsync();

        // include UTC timestamp so clients render the same moment
        await Clients.Others.SendAsync("ReceiveMessage", entity.User, entity.Text, entity.Avatar, entity.SentAtUtc);
    }
}
