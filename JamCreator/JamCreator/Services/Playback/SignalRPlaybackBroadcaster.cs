using System.Threading.Tasks;
using JamCreator.Shared.Models.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace JamCreator.Services.Playback
{
    public sealed class SignalRPlaybackBroadcaster : IPlaybackBroadcaster
    {
        private readonly IHubContext<PlaybackHub> _hubContext;

        public SignalRPlaybackBroadcaster(IHubContext<PlaybackHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task BroadcastAsync(PlaybackState state)
        {
            var dto = new PlaybackStateDto
            {
                SessionId = state.SessionId,
                TrackId = state.TrackId,
                Status = state.Status,
                PositionSeconds = state.Position.TotalSeconds,
                LastUpdatedUtc = state.LastUpdatedUtc
            };

            return _hubContext.Clients.Group(state.SessionId.ToString())
                .SendAsync("PlaybackChanged", dto);
        }
    }
}
