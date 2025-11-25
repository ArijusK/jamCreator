namespace JamCreator.Shared.Models.DTOs;

public class UserProfileDto
{
    public string Id { get; set; } = default!;
    public string Username { get; set; } = string.Empty;
    public string FavoriteGenre { get; set; } = string.Empty;
    public string Avatar { get; set; } = "🎸";
    public DateTime UpdatedAtUtc { get; set; }
}
