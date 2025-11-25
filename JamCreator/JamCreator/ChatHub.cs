using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

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
    private static readonly ConcurrentDictionary<string, string> _connectedUsers =
        new ConcurrentDictionary<string, string>();

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
        _connectedUsers.AddOrUpdate(Context.ConnectionId, user, (k, v) => user);

        var log = new AuditLogEntry 
        { 
            Action = "Message", 
            User = user, 
            Details = $"Sent to {sessionId}: {message}" 
        };
        LogAction(log);

        await Clients.Group(sessionId).SendAsync("ReceiveMessage", user, message, avatar);
    }

    private void LogAction<T>(T info) where T : class, new()
    {
        var envelope = new DataEnvelope<T>(info);
        Console.WriteLine($"[AUDIT {envelope.Timestamp}]: {envelope.DataType} -> Action performed.");
    }
}