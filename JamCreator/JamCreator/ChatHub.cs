using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public interface IChatPayload
{
    string User { get; }
    string Message { get; }
    string Avatar { get; }
}

public class ChatEnvelope<TPayload, TMetadata>
    where TPayload : IChatPayload, new()
    where TMetadata : class
{
    public TPayload Payload { get; set; } = new();
    public TMetadata? Metadata { get; set; }
}

public class ChatEnvelopeDto
{
    public string User { get; set; } = "";
    public string Message { get; set; } = "";
    public string Avatar { get; set; } = "";
    public object? Metadata { get; set; }
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

    public async Task SendMessage(string user, string message, string avatar)
    {
        _connectedUsers.AddOrUpdate(Context.ConnectionId, user, (k, v) => user);
        await Clients.Others.SendAsync("ReceiveMessage", user, message, avatar);
    }

    public async Task SendEnvelope(ChatEnvelopeDto envelope)
    {
        _connectedUsers.AddOrUpdate(Context.ConnectionId, envelope.User, (k, v) => envelope.User);
        await Clients.Others.SendAsync(
            "ReceiveMessage",
            envelope.User,
            envelope.Message,
            envelope.Avatar
        );
    }

    public Task<List<string>> GetOnlineUsers()
    {
        return Task.FromResult(_connectedUsers.Values.Distinct().ToList());
    }
}
