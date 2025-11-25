using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using JamCreator.Shared.Interfaces;

namespace JamCreator.Shared.Models
{
    public class JamSessionModel:IEntity<string>
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MaxLength(100)] public string RoomName { get; set; } = default!;
        [MaxLength(60)] public string? Genre { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        [MaxLength(100)] public string? Password { get; set; }
        [MaxLength(60)] public JamMood Mood { get; set; } = JamMood.Chill;
        public int? MaxPeople { get; set; } = 4;
        public int? DurationMinutes { get; set; }
        public bool AllowSkipVote { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<SessionParticipant> Participants { get; set; } = new();
        public List<AudioTrack> Tracks { get; set; } = new();

        // Concurrency (optional—but recommended)
        [Timestamp] public byte[]? RowVersion { get; set; }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public string? TempPassword { get; set; }
    }
}



public class JamSession
{

}

