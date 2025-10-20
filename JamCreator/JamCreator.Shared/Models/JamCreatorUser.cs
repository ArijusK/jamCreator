using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamCreator.Shared.Models
{
    public class JamCreatorUser
    {
        public string? RoomName { get; set; }
        public int? MaxPeople { get; set; }
        public string? Genre { get; set; }
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public string? Password { get; set; }
        public string Mood { get; set; } = "Chill";
        public int DurationMinutes { get; set; } = 60;
        public bool AllowSkipVote { get; set; }
    }
}