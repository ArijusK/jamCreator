using System;
using JamCreator.Shared.Models;

namespace JamCreator.Shared.Models.DTOs
{
    public class PlaybackStateDto
    {
        public Guid SessionId { get; set; }
        public Guid TrackId { get; set; }
        public PlaybackStatus Status { get; set; }
        public double PositionSeconds { get; set; }
        public DateTimeOffset LastUpdatedUtc { get; set; }
    }
}
