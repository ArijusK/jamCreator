using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace JamCreator.Shared.Models
{
    public class JamSessionModel
    {
        public string Id { get; set; } = default!;
        public string RoomName { get; set; } = default!;
        public int MaxPeople { get; set; }
        public string? Genre { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public string? Password { get; set; }
        public JamMood Mood { get; set; } = JamMood.Chill;
        public int DurationMinutes { get; set; }
        public bool AllowSkipVote { get; set; }


        [System.Text.Json.Serialization.JsonIgnore]
        public string? TempPassword { get; set; }
    }
}
