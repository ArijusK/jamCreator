namespace JamCreator.Shared.Models.DTOs
{
    public class ChatMessageDto
    {
        public string User { get; set; } = default!;
        public string Text { get; set; } = default!;
        public string? Avatar { get; set; }
        public DateTime SentAtUtc { get; set; }
    }
}
