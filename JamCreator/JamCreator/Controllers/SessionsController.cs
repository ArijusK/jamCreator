using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using JamCreator.Shared.Models;          // JamSessionModel, SessionParticipant, AudioTrack
using JamCreator.Shared.Models.DTOs;     // JamSessionDto, ParticipantDto, AudioTrackDto
using JamCreator.Shared.Interfaces;      // IRepository<T,TKey>
using JamCreator.Data;                   // AppDbContext
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
            // Projekcija į DTO per DbContext (efektyvu ir išvengia ciklų)
            var list = await _db.JamSessions
                .AsNoTracking()
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

            var dto = await _db.JamSessions
                .Where(s => s.Id == id)
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
                            Duration = t.Duration
                        }).ToList()
                })
                .AsNoTracking()
                .AsSplitQuery() // jei yra kelios kolekcijos – padalina užklausas
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
            if (session is null) return NotFound("Session not found.");
            if (session.IsPrivate && session.Password != request.Password)
                return BadRequest("Incorrect password.");

            await _participants.AddAsync(new SessionParticipant
            {
                JamSessionId = session.Id,
                DisplayName  = request.DisplayName ?? "Guest"
            }, ct);

            // Grąžiname mažą DTO
            return Ok(new { session.Id, session.RoomName });
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

        // DELETE: api/sessions/delete-session/{id}
        [HttpDelete("delete-session/{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Missing id.");

            var ok = await _sessions.DeleteByIdAsync(id, ct);
            return ok ? NoContent() : NotFound("Session not found.");
        }
    }
}
