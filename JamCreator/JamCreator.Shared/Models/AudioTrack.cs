using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JamCreator.Shared.Interfaces;

namespace JamCreator.Shared.Models
{
    public class AudioTrack:IEntity<int>
    {
        public int Id { get; set; }

        [Required] public string JamSessionId { get; set; } = default!;

        [JsonIgnore] public JamSessionModel JamSession { get; set; }

        // store the file name relative to wwwroot/audio 
        [Required, MaxLength(260)] public string FileName { get; set; } = default!;

        [MaxLength(120)] public string? Title { get; set; }

        public TimeSpan? Duration { get; set; } 
        public JamMood Mood { get; set; }
        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    }
}