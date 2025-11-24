using System;
using System.Threading.Tasks;

namespace JamCreator.Services.Playback
{
    public interface IPlaybackBroadcaster
    {
        Task BroadcastAsync(PlaybackState state);
    }
}
