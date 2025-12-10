using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamCreator.Shared.Models.DTOs
{
    public class JamSessionDto
    {
        public string Id { get; set; } = default!;
        public string RoomName { get; set; } = default!;
        public string? Genre { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public JamMood? Mood { get; set; }
        public int? MaxPeople { get; set; }
        public int? DurationMinutes { get; set; }
        public bool AllowSkipVote { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc
            => DurationMinutes.HasValue
                ? CreatedAtUtc.AddMinutes(DurationMinutes.Value)
                : null;

        public List<ParticipantDto> Participants { get; set; } = new();
        public List<AudioTrackDto> Tracks { get; set; } = new();
    }
}