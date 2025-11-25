using System.ComponentModel.DataAnnotations;
using JamCreator.Shared.Interfaces;

namespace JamCreator.Shared.Models;

public class UserProfile : IEntity<string>
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N"); // clientId from localStorage

    [Required(ErrorMessage = "Nickname is required"), MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FavoriteGenre { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Avatar { get; set; } = "🎸";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
