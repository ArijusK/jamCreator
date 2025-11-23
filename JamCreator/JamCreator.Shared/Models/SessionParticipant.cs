using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JamCreator.Shared.Interfaces;

namespace JamCreator.Shared.Models
{
    public class SessionParticipant:IEntity<int>
    {
        public int Id { get; set; }

        [Required] public string JamSessionId { get; set; } = default!;
        [JsonIgnore] public JamSessionModel JamSession { get; set; } = null!;
        [Required, MaxLength(100)] public string DisplayName { get; set; } = default!;
        [MaxLength(100)]
        public string? ClientToken { get; set; }

        public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    }
}