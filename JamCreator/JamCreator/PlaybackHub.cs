using System;
using System.Threading.Tasks;
using JamCreator.Services.Playback;
using Microsoft.AspNetCore.SignalR;

public class PlaybackHub : Hub
{
    private readonly IPlaybackCoordinator _coordinator;

    public PlaybackHub(IPlaybackCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public Task JoinSession(string sessionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task Play(string sessionId, string trackId, double positionSeconds)
    {
        var sId = Guid.Parse(sessionId);
        var tId = Guid.Parse(trackId);

        // Coordinator handles state AND broadcasting
        await _coordinator.PlayAsync(sId, tId, TimeSpan.FromSeconds(positionSeconds));
    }

    public async Task Pause(string sessionId, double positionSeconds)
    {
        var sId = Guid.Parse(sessionId);

        // Coordinator handles broadcasting
        await _coordinator.PauseAsync(sId, TimeSpan.FromSeconds(positionSeconds));
    }

    public async Task Stop(string sessionId)
    {
        var sId = Guid.Parse(sessionId);

        // Coordinator handles broadcasting
        await _coordinator.StopAsync(sId);
    }
}
