using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamCreator.Shared.Models.DTOs
{
    public class ParticipantDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = default!;
        public DateTime JoinedAtUtc { get; set; }
    }
}