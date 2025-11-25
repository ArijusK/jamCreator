using JamCreator.Data;
using JamCreator.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace JamCreator.Services
{
    public class AudioMoodService : IAudioMoodService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AudioMoodService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task AssignTracksAsync(JamSessionModel session, CancellationToken ct)
        {
            // audio/chill/ , audio/rock/ ...
            var moodFolder = Path.Combine(
                _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                "audio",
                session.Mood.ToString().ToLower()
            );

            if (!Directory.Exists(moodFolder))
                return;

            var files = Directory.GetFiles(moodFolder, "*.mp3", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                var track = new AudioTrack
                {
                    JamSessionId = session.Id,
                    FileName = Path.GetFileName(file),
                    Title = Path.GetFileNameWithoutExtension(file),
                    Mood = session.Mood,
                    AddedAtUtc = DateTime.UtcNow
                };

                _db.Tracks.Add(track);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
