namespace JamCreator.Models;

public class JamCreatorUser
{
    public string? RoomName { get; set; }
    public int? MaxPeople { get; set; }
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public string? Password { get; set; }
    public string? Mood { get; set; }
    public int DurationMinutes { get; set; }
    public bool AllowSkipVote { get; set; }
}

public class JamSession
{
    public string Id { get; set; } = default!;
    public string RoomName { get; set; } = default!;
    public int MaxPeople { get; set; }
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public string? Mood { get; set; }
    public int DurationMinutes { get; set; }
    public bool AllowSkipVote { get; set; }
    public List<string> SkipVotes { get; set; } = new();
}