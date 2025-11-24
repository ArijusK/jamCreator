using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using JamCreator.Data;
using JamCreator.Shared.Models;

public class DataEnvelope<TData>
    where TData : class, new()
{
    public TData Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DataType { get; set; }

    public DataEnvelope(TData data)
    {
        Data = data;
        DataType = typeof(TData).Name;
    }
}

public class AuditLogEntry
{
    public string Action { get; set; } = "";
    public string User { get; set; } = "";
    public string Details { get; set; } = "";
}

public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private static readonly ConcurrentDictionary<string, string> _connectedUsers = new();

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        _connectedUsers.TryAdd(Context.ConnectionId, "Unknown");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectedUsers.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        var log = new AuditLogEntry
        {
            Action = "Join",
            User = Context.ConnectionId,
            Details = $"Joined room {sessionId}"
        };
        LogAction(log);
    }

    public async Task SendMessage(string user, string message, string avatar, string sessionId)
    {
        var cleanUser   = string.IsNullOrWhiteSpace(user) ? "Guest" : user.Trim();
        var cleanText   = message?.Trim() ?? "";
        var cleanAvatar = string.IsNullOrWhiteSpace(avatar) ? null : avatar.Trim();

        if (string.IsNullOrWhiteSpace(cleanText))
            return;

        // 🔹 įrašom į DB su SessionId
        var entity = new ChatMessage
        {
            User       = cleanUser,
            Text       = cleanText,
            Avatar     = cleanAvatar,
            SessionId  = sessionId,
            SentAtUtc  = DateTime.UtcNow
        };

        _db.ChatMessages.Add(entity);
        await _db.SaveChangesAsync();

        _connectedUsers.AddOrUpdate(Context.ConnectionId, cleanUser, (_, _) => cleanUser);

        var log = new AuditLogEntry
        {
            Action = "Message",
            User   = cleanUser,
            Details = $"Sent to {sessionId}: {cleanText}"
        };
        LogAction(log);

        // 🔹 siunčiam visiems tame room’e, kartu ir timestamp’ą
        await Clients.Group(sessionId).SendAsync(
            "ReceiveMessage",
            entity.User,
            entity.Text,
            entity.Avatar,
            entity.SentAtUtc
        );
    }

    private void LogAction<T>(T info) where T : class, new()
    {
        var envelope = new DataEnvelope<T>(info);
        Console.WriteLine($"[AUDIT {envelope.Timestamp:u}]: {envelope.DataType} -> action logged.");
    }
}
