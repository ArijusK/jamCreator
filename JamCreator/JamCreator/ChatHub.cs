using Microsoft.AspNetCore.SignalR;

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

    public async Task SendMessage(string user, string message, string avatar)
    {
        await Clients.Others.SendAsync("ReceiveMessage", user, message, avatar);
    }

    public async Task SendEnvelope(ChatEnvelopeDto envelope)
    {
        await Clients.Others.SendAsync(
            "ReceiveMessage",
            envelope.User,
            envelope.Message,
            envelope.Avatar
        );
    }
}
