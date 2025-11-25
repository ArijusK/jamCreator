using System.ComponentModel.DataAnnotations;

namespace JamCreator.Shared.Models;

public class JoinModel
{
        public string SessionId { get; set; } = "";
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
        public string? ClientToken { get; set; }
}
