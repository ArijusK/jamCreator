using Microsoft.AspNetCore.SignalR;

    public class MusicHub : Hub
    {
        // 1) Kai klientas įeina į sesiją – joininam jį į SignalR grupę
        public Task JoinSessionGroup(string sessionId)
            => Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        // 2) Išėjimas iš grupės (naudinga jei darysi leave)
        public Task LeaveSessionGroup(string sessionId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

        // 3) Groti dainą (iš vieno kliento siunčiam visiems kitiems)
        public Task BroadcastPlayTrack(string sessionId, string trackKey, double positionSeconds)
        {
            // ŠITA eilutė paleidžia muziką visiems KITIEMS klientams
            return Clients
                .OthersInGroup(sessionId)
                .SendAsync("PlayTrack", trackKey, positionSeconds);
        }
        public Task BroadcastPauseTrack(string sessionId, string trackKey, double positionSeconds)
        {
            return Clients
                .OthersInGroup(sessionId)
                .SendAsync("PauseTrack", trackKey, positionSeconds);
        }
    }

