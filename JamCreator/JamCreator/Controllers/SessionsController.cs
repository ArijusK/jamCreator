using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using JamCreator.Shared.Models;         
using JamCreator.Shared.Models.DTOs;     
using JamCreator.Shared.Interfaces;      
using JamCreator.Data;                   
using JamCreator.Services; 
                                         
namespace JamCreator.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly IRepository<JamSessionModel, string> _sessions;
        private readonly IRepository<SessionParticipant, int> _participants;
        private readonly IRepository<AudioTrack, int> _tracks;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IAudioMoodService _audioMood;
        public SessionsController(
            IRepository<JamSessionModel, string> sessions,
            IRepository<SessionParticipant, int> participants,
            IRepository<AudioTrack, int> tracks,
            AppDbContext db,
            IWebHostEnvironment env,
            IAudioMoodService audioMood)
        {
            _sessions = sessions;
            _participants = participants;
            _tracks = tracks;
            _db = db;
            _env = env;
            _audioMood = audioMood;
            
        }

        private static bool IsExpired(JamSessionModel s)
        {
            if (!s.DurationMinutes.HasValue) return false;

            var expires = s.CreatedAtUtc.AddMinutes(s.DurationMinutes.Value);
            return DateTime.UtcNow >= expires;
        }

        private async Task CleanupExpiredSessionsAsync()
        {
            var now = DateTime.UtcNow;

            var expired = await _db.JamSessions
                .Where(s => s.DurationMinutes != null &&
                            s.CreatedAtUtc.AddMinutes(s.DurationMinutes.Value) <= now)
                .ToListAsync();

            if (expired.Count == 0)
                return;

            _db.JamSessions.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }


        // POST: api/sessions/create-jam
        [HttpPost("create-jam")]
        public async Task<IActionResult> Create([FromBody] JamCreateModel model, CancellationToken ct)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.RoomName))
                return BadRequest("Invalid session data");

            var session = new JamSessionModel
            {
                RoomName        = model.RoomName,
                Genre           = model.Genre,
                Description     = model.Description,
                IsPrivate       = model.IsPrivate,
                Password        = model.Password,
                Mood            = model.Mood,
                MaxPeople       = model.MaxPeople ?? 4,
                DurationMinutes = model.DurationMinutes,
                AllowSkipVote   = model.AllowSkipVote
            };

            await _sessions.AddAsync(session, ct);
            await _audioMood.AssignTracksAsync(session, ct);
            return Created($"/api/sessions/get-session-id/{session.Id}", session.Id);
        }

        // GET: api/sessions/get-sessions
[HttpGet("get-sessions")]
public async Task<ActionResult<IEnumerable<JamSessionDto>>> GetAll(CancellationToken ct)
{
    var nowUtc = DateTime.UtcNow;

    var list = await _db.JamSessions
        .AsNoTracking()
        
        .Where(s =>
            !s.DurationMinutes.HasValue ||
            s.DurationMinutes <= 0 ||
            s.CreatedAtUtc.AddMinutes(s.DurationMinutes.Value) > nowUtc
        )
        .OrderByDescending(s => s.CreatedAtUtc)
        .Select(s => new JamSessionDto
        {
            Id              = s.Id,
            RoomName        = s.RoomName,
            Genre           = s.Genre,
            Description     = s.Description,
            IsPrivate       = s.IsPrivate,
            Mood            = s.Mood,
            MaxPeople       = s.MaxPeople,
            DurationMinutes = s.DurationMinutes,
            AllowSkipVote   = s.AllowSkipVote,
            CreatedAtUtc    = s.CreatedAtUtc
        })
        .ToListAsync(ct);

    return Ok(list);
}




        // GET: api/sessions/get-session-id/{id}
        [HttpGet("get-session-id/{id}")]
        public async Task<ActionResult<JamSessionDto>> GetById(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            await CleanupExpiredSessionsAsync();
            var now = DateTime.UtcNow;

            var dto = await _db.JamSessions
                .Where(s => s.Id == id &&
                            (s.DurationMinutes == null ||
                            s.CreatedAtUtc.AddMinutes(s.DurationMinutes.Value) > now))
                .Select(s => new JamSessionDto
                {
                    Id              = s.Id,
                    RoomName        = s.RoomName,
                    Genre           = s.Genre,
                    Description     = s.Description,
                    IsPrivate       = s.IsPrivate,
                    Mood            = s.Mood,
                    MaxPeople       = s.MaxPeople,
                    DurationMinutes = s.DurationMinutes,
                    AllowSkipVote   = s.AllowSkipVote,
                    CreatedAtUtc    = s.CreatedAtUtc,
                    Participants    = s.Participants
                        .Select(p => new ParticipantDto
                        {
                            Id          = p.Id,
                            DisplayName = p.DisplayName,
                            JoinedAtUtc = p.JoinedAtUtc
                        }).ToList(),
                    Tracks = s.Tracks
                        .Select(t => new AudioTrackDto
                        {
                            Id       = t.Id,
                            FileName = t.FileName,
                            Title    = t.Title,
                            Mood     = t.Mood,
                            Duration = t.Duration,
                            IsCustom = t.IsCustom 
                        }).ToList()
                })
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(ct);

            return dto is null ? NotFound() : Ok(dto);
        }


        // POST: api/sessions/join-jam
[HttpPost("join-jam")]
public async Task<IActionResult> Join([FromBody] JoinModel request, CancellationToken ct)
{
    if (request is null || string.IsNullOrWhiteSpace(request.SessionId))
        return BadRequest("Invalid join request.");

    var session = await _sessions.GetByIdAsync(request.SessionId, ct);
    if (session is null)
        return NotFound("Session not found.");

    if (session.IsPrivate && session.Password != request.Password)
        return BadRequest("Incorrect password.");

    var nowUtc = DateTime.UtcNow;
    if (session.DurationMinutes.HasValue &&
        session.DurationMinutes > 0 &&
        session.CreatedAtUtc.AddMinutes(session.DurationMinutes.Value) <= nowUtc)
    {
        return BadRequest("Session has expired.");
    }

    var name = string.IsNullOrWhiteSpace(request.DisplayName)
        ? "Guest"
        : request.DisplayName.Trim();

    var participants = await _participants.ListAsync(
        p => p.JamSessionId == session.Id,
        ct
    );

    var maxPeople = session.MaxPeople ?? int.MaxValue;

    SessionParticipant? sameClient = null;
    if (!string.IsNullOrWhiteSpace(request.ClientToken))
    {
        sameClient = participants
            .FirstOrDefault(p => p.ClientToken == request.ClientToken);
    }

    if (sameClient is not null)
    {
        if (!string.Equals(sameClient.DisplayName, name, StringComparison.Ordinal))
        {
            sameClient.DisplayName = name;
            await _participants.UpdateAsync(sameClient, ct);
        }

        return Ok(new
        {
            session.Id,
            session.RoomName,
            JoinedAs     = name,
            AlreadyJoined = true
        });
    }

    if (participants.Count >= maxPeople)
        return BadRequest("Session is full.");

    var newParticipant = new SessionParticipant
    {
        JamSessionId = session.Id,
        DisplayName  = name,
        ClientToken  = request.ClientToken
    };

    await _participants.AddAsync(newParticipant, ct);

    return Ok(new
    {
        session.Id,
        session.RoomName,
        JoinedAs      = name,
        AlreadyJoined = false
    });
}




        [HttpGet("play-audio/{mood}/{fileName}")]
        public IActionResult PlayAudio(string mood, string fileName)
        {
            if (string.IsNullOrWhiteSpace(mood) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest();

            fileName = Path.GetFileName(fileName);

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, "audio", mood.ToLower(), fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File not found: {filePath}");

            return new PhysicalFileResult(filePath, "audio/mpeg")
            {
                EnableRangeProcessing = true
            };
        }

        // GET: api/sessions/play-audio/custom/{sessionId}/{fileName}
        [HttpGet("play-audio/custom/{fileName}")]
        public IActionResult PlayCustomAudio(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest();

             var safeName = Path.GetFileName(fileName);

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, "audio", "custom", safeName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File not found: {filePath}");

            return new PhysicalFileResult(filePath, "audio/mpeg")
            {
                EnableRangeProcessing = true
            };
        }

        [HttpPost("{sessionId}/upload-track")]
        public async Task<IActionResult> UploadAudio(string sessionId, IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .mp3 files are allowed.");

            var session = await _db.JamSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is null)
                return NotFound("Session not found.");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder  = Path.Combine(webRoot, "audio", "custom");
            Directory.CreateDirectory(folder);

            var safefileName = Path.GetFileName(file.FileName);
            var savePath = Path.Combine(folder, safefileName);

            using (var stream = System.IO.File.Create(savePath))
                await file.CopyToAsync(stream, ct);

            var track = new AudioTrack
            {
                JamSessionId = session.Id,                         
                FileName      = safefileName,                          
                Title         = Path.GetFileNameWithoutExtension(safefileName),
                Mood          = session.Mood,
                IsCustom      = true,
                AddedAtUtc    = DateTime.UtcNow
            };

            _db.Tracks.Add(track);  
            await _db.SaveChangesAsync(ct);

            return Ok(new AudioTrackDto
            {
                Id       = track.Id,
                FileName = track.FileName,
                Title    = track.Title,
                Mood     = track.Mood,
                IsCustom = true
            });
        }

        [HttpDelete("{trackId}/delete-track")]
        public async Task<IActionResult> DeleteTrack(int trackId, CancellationToken ct)
        {
            var track = await _db.Tracks.FirstOrDefaultAsync(t => t.Id == trackId, ct);
            if (track is null)
                return NotFound("Track not found.");

            if (track.IsCustom)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(webRoot, "audio", "custom", track.FileName);

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _db.Tracks.Remove(track);
            await _db.SaveChangesAsync(ct);

            return Ok(new { success = true });
        }




        // DELETE: api/sessions/delete-session/{id}
        [HttpDelete("delete-session/{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing id.");

            var session = await _sessions.GetByIdAsync(id, ct);
            if (session is null)
                return NotFound("Session not found.");

            var customTracks = await _db.Tracks
                .Where(t => t.JamSessionId == id && t.IsCustom)
                .ToListAsync(ct);

            var webRoot     = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var customFolder = Path.Combine(webRoot, "audio", "custom");

            foreach (var track in customTracks)
            {
                if (string.IsNullOrWhiteSpace(track.FileName))
                    continue;

                var path = Path.Combine(customFolder, track.FileName);

                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        System.IO.File.Delete(path);
                    }
                    catch
                    {

                    }
                }
            }

            var ok = await _sessions.DeleteByIdAsync(id, ct);
            if (!ok)
                return NotFound("Session not found.");

            return NoContent();
        }

        
        [HttpPost("leave-jam")]
        public async Task<IActionResult> Leave([FromBody] LeaveJamModel req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.SessionId))
                return BadRequest();

            var participants = await _participants.ListAsync(
                p => p.JamSessionId == req.SessionId &&
                    p.DisplayName == req.DisplayName,
                ct
            );

            if (!participants.Any())
                return NoContent();

            foreach (var p in participants)
                await _participants.DeleteAsync(p, ct);

            return NoContent();
        }

        // GET: api/sessions/{sessionId}/participants
        [HttpGet("{sessionId}/participants")]
        public async Task<IActionResult> GetParticipants(string sessionId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest("Missing session id.");

            var session = await _sessions.GetByIdAsync(sessionId, ct);
            if (session is null)
                return NotFound("Session not found.");

            var participants = await _participants.ListAsync(
                p => p.JamSessionId == sessionId,
                ct
            );

            var result = participants
                .Select(p => p.DisplayName)
                .ToList();

            return Ok(result);
        }
    }
}
