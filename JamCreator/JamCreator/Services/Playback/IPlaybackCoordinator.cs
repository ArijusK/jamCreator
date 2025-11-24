using System;
using System.Threading.Tasks;

namespace JamCreator.Services.Playback
{
    public interface IPlaybackCoordinator
    {
        Task<PlaybackState> GetStateAsync(Guid sessionId);
        Task<PlaybackState> PlayAsync(Guid sessionId, Guid trackId, TimeSpan position);
        Task<PlaybackState> PauseAsync(Guid sessionId, TimeSpan position);
        Task<PlaybackState> StopAsync(Guid sessionId);
    }
}
