using System;
using System.Threading.Tasks;

namespace JamCreator.Services.Playback
{
    public interface IPlaybackStateStore
    {
        Task<PlaybackState?> GetAsync(Guid sessionId);
        Task SaveAsync(PlaybackState state);
    }
}
