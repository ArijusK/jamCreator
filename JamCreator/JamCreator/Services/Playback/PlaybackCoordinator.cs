using System;
using System.Threading.Tasks;

namespace JamCreator.Services.Playback
{
    public sealed class PlaybackCoordinator : IPlaybackCoordinator
    {
        private readonly IPlaybackStateStore _store;
        private readonly IPlaybackBroadcaster _broadcaster;

        public PlaybackCoordinator(IPlaybackStateStore store, IPlaybackBroadcaster broadcaster)
        {
            _store = store;
            _broadcaster = broadcaster;
        }

        public async Task<PlaybackState> GetStateAsync(Guid sessionId)
        {
            var state = await _store.GetAsync(sessionId);
            if (state == null)
            {
                state = new PlaybackState(sessionId, Guid.Empty);
                await _store.SaveAsync(state);
            }
            return state;
        }

        public async Task<PlaybackState> PlayAsync(Guid sessionId, Guid trackId, TimeSpan position)
        {
            var state = await GetStateAsync(sessionId);
            state.ApplyPlay(trackId, position);

            await _store.SaveAsync(state);
            await _broadcaster.BroadcastAsync(state);
            return state;
        }

        public async Task<PlaybackState> PauseAsync(Guid sessionId, TimeSpan position)
        {
            var state = await GetStateAsync(sessionId);
            state.ApplyPause(position);

            await _store.SaveAsync(state);
            await _broadcaster.BroadcastAsync(state);
            return state;
        }

        public async Task<PlaybackState> StopAsync(Guid sessionId)
        {
            var state = await GetStateAsync(sessionId);
            state.Stop();

            await _store.SaveAsync(state);
            await _broadcaster.BroadcastAsync(state);
            return state;
        }
    }
}
