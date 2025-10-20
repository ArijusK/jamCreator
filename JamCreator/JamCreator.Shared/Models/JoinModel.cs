using System.ComponentModel.DataAnnotations;

namespace JamCreator.Shared.Models;

public class JoinModel
{
        [Required]
        public string? SessionId { get; set; }
        public string? Password { get; set; }
}
