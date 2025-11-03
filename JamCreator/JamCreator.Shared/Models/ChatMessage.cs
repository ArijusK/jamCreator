using System;
using System.ComponentModel.DataAnnotations;
using JamCreator.Shared.Interfaces;

namespace JamCreator.Shared.Models
{
    public class ChatMessage : IEntity<int>
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string User { get; set; } = default!;

        [Required, MaxLength(2000)]
        public string Text { get; set; } = default!;

        [MaxLength(16)]
        public string? Avatar { get; set; }

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    }
}
