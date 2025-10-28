using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace JamCreator.Shared.Models
{

    public class JamCreateModel : IValidatableObject
    {
        [StringLength(60)]
        public string? RoomName { get; set; }

        [Range(1, 4)]
        public int? MaxPeople { get; set; }

        [StringLength(40)]
        public string? Genre { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsPrivate { get; set; }

        [StringLength(40)]
        public string? Password { get; set; }

        [Required]
        public JamMood Mood { get; set; } = JamMood.Chill;

        [Range(15, 240)]
        public int DurationMinutes { get; set; } = 60;

        public bool AllowSkipVote { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (IsPrivate && string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(
                    "Password is required for private sessions.",
                    new[] { nameof(Password) });
            }
        }
    }
}