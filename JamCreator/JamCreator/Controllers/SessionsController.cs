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
        //Dependency Injection everywhere where reasonable
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
                            Duration = t.Duration,
                            IsCustom = t.IsCustom 
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
            if (session is null)
                return NotFound("Session not found.");

            if (session.IsPrivate && session.Password != request.Password)
                return BadRequest("Incorrect password.");

            var name = string.IsNullOrWhiteSpace(request.DisplayName)
                ? "Guest"
                : request.DisplayName.Trim();

            var participants = await _participants.ListAsync(
                p => p.JamSessionId == session.Id,
                ct
            );

            var maxPeople = session.MaxPeople ?? int.MaxValue;

            // 👇 Surandam, ar šitas klientas jau yra prisijungęs prie šito jam'o
            SessionParticipant? sameClient = null;
            if (!string.IsNullOrWhiteSpace(request.ClientToken))
            {
                sameClient = participants
                    .FirstOrDefault(p => p.ClientToken == request.ClientToken);
            }

            if (sameClient is not null)
            {
                // Tas pats klientas grįžo – atnaujinam vardą, jei pasikeitė
                if (!string.Equals(sameClient.DisplayName, name, StringComparison.Ordinal))
                {
                    sameClient.DisplayName = name;
                    await _participants.UpdateAsync(sameClient, ct);
                }

                return Ok(new
                {
                    session.Id,
                    session.RoomName,
                    JoinedAs = name,
                    AlreadyJoined = true
                });
            }

            // Naujas klientas – tikrinam, ar nėra full
            if (participants.Count >= maxPeople)
                return BadRequest("Session is full.");

            // Nebedarom draudimo „tas pats vardas“ – ribojam pagal ClientToken
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
                JoinedAs = name,
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

             var safeName = Path.GetFileName(fileName); // apsauga

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            // pvz: wwwroot/audio/custom/{sessionId}/{fileName}
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

            // ✅ išsaugom su originaliu failo vardu ir plėtiniu (.mp3)
            var safefileName = Path.GetFileName(file.FileName);
            var savePath = Path.Combine(folder, safefileName);

            using (var stream = System.IO.File.Create(savePath))
                await file.CopyToAsync(stream, ct);

            // ✅ ČIA TURI BŪTI ENTITY, NE DTO
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

            // Tik custom dainas šalinam iš disko
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

            // 1) randam sessioną
            var session = await _sessions.GetByIdAsync(id, ct);
            if (session is null)
                return NotFound("Session not found.");

            // 2) susikraunam visus custom trackus šitam session
            var customTracks = await _db.Tracks
                .Where(t => t.JamSessionId == id && t.IsCustom)
                .ToListAsync(ct);

            // 3) failų kelias: wwwroot/audio/custom/{FileName}
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
                        // čia, jei nori, gali prisiloginti klaidą į failą ar loggerį
                    }
                }
            }

            // 4) trinam pačią session (cascade DB'e ištrins ir Tracks įrašus)
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
