using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamCreator.Shared.Models.DTOs
{
    public class AudioTrackDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = default!;
        public string? Title { get; set; }
        public JamMood Mood { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool IsCustom { get; set; }
    }
}