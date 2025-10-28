using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message, string avatar)
    {
        await Clients.Others.SendAsync("ReceiveMessage", user, message, avatar);
    }
}
