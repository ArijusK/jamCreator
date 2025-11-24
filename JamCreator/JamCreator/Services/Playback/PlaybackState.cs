using System;
using JamCreator.Shared.Models; // for PlaybackStatus

namespace JamCreator.Services.Playback
{
    public sealed class PlaybackState
    {
        public Guid SessionId { get; }
        public Guid TrackId { get; private set; }
        public PlaybackStatus Status { get; private set; }
        public TimeSpan Position { get; private set; }
        public DateTimeOffset LastUpdatedUtc { get; private set; }

        public PlaybackState(Guid sessionId, Guid trackId)
        {
            SessionId = sessionId;
            TrackId = trackId;
            Status = PlaybackStatus.Stopped;
            Position = TimeSpan.Zero;
            LastUpdatedUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyPlay(Guid trackId, TimeSpan position)
        {
            TrackId = trackId;
            Position = position;
            Status = PlaybackStatus.Playing;
            LastUpdatedUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyPause(TimeSpan position)
        {
            Position = position;
            Status = PlaybackStatus.Paused;
            LastUpdatedUtc = DateTimeOffset.UtcNow;
        }

        public void Stop()
        {
            Status = PlaybackStatus.Stopped;
            Position = TimeSpan.Zero;
            LastUpdatedUtc = DateTimeOffset.UtcNow;
        }
    }
}
