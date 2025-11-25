namespace JamCreator.Shared.Models.DTOs
{
    public class ChatHistoryDto
    {
        public string User { get; set; } = "";
        public string Text { get; set; } = "";
        public string? Avatar { get; set; }
        public DateTime SentAtUtc { get; set; }
    }
}
