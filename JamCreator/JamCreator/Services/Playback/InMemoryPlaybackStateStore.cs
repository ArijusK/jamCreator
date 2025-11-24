using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace JamCreator.Services.Playback
{
    public sealed class InMemoryPlaybackStateStore : IPlaybackStateStore
    {
        private readonly ConcurrentDictionary<Guid, PlaybackState> _states = new();

        public Task<PlaybackState?> GetAsync(Guid sessionId)
        {
            _states.TryGetValue(sessionId, out var state);
            return Task.FromResult(state);
        }

        public Task SaveAsync(PlaybackState state)
        {
            _states[state.SessionId] = state;
            return Task.CompletedTask;
        }
    }
}
