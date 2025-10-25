using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using JamCreator.Shared.Models;
using System.Text.Json;
using System.IO;
using JamCreator.Services;


namespace JamCreator.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
       // private readonly string _sessionsFilePath;
        //private readonly JsonSerializerOptions _jsonOptions;
        private readonly FileSessionStore _store;
        //private string? audioBase64;
        private readonly IWebHostEnvironment _env;

        public SessionsController(IWebHostEnvironment env, FileSessionStore store)
        {
            //_sessionsFilePath = Path.Combine(env.ContentRootPath, "sessions.json");
            //_jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            _env = env;
            _store = store;
        }
///----------------------------------------------------------------------------------///
//move to services on server
        // Helper: load sessions from file
        /*private List<JamSessionModel> LoadSessions()
        {
            if (!System.IO.File.Exists(_sessionsFilePath))
            {
                Console.WriteLine($"File contents1");
                return new List<JamSessionModel>();
            }

            var json = System.IO.File.ReadAllText(_sessionsFilePath);
            Console.WriteLine($"File contents2");
            return JsonSerializer.Deserialize<List<JamSessionModel>>(json) ?? new();
        }

        // Helper: save sessions to file
        private void SaveSessions(List<JamSessionModel> sessions)
        {
            var json = JsonSerializer.Serialize(sessions, _jsonOptions);
            Console.WriteLine($"[SaveSessions] Writing to {_sessionsFilePath}");
            System.IO.File.WriteAllText(_sessionsFilePath, json);
        }*/

        ///----------------------------------------------------------------------------------///

        // 🟢 POST: api/sessions
        [HttpPost("create-jam")]
        public IActionResult Create([FromBody] JamCreateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.RoomName))
                return BadRequest("Invalid session data");

            var sessions = _store.LoadSessions();

            var newSession = new JamSessionModel
            {
                Id = Guid.NewGuid().ToString("N"),
                RoomName = model.RoomName,
                Genre = model.Genre,
                Description = model.Description,
                IsPrivate = model.IsPrivate,
                Password = model.Password,
                Mood = model.Mood,
                MaxPeople = model.MaxPeople ?? 4,
                DurationMinutes = model.DurationMinutes,
                AllowSkipVote = model.AllowSkipVote
            };

            sessions.Add(newSession);
            _store.SaveSessions(sessions);

            return Created($"/api/session/{newSession.Id}", newSession.Id);
        }

        // 🟡 GET: api/sessions
        [HttpGet("get-sessions")]
        public IActionResult GetAll()
        {
            var sessions = _store.LoadSessions();
            return Ok(sessions);
        }

        // 🔵 GET: api/sessions/{id}
        [HttpGet("get-session-id/{id}")]
        public IActionResult GetById(string id)
        {
            var sessions = _store.LoadSessions();
            var session = sessions.FirstOrDefault(s => s.Id == id);
            if (session == null)
                return NotFound();
            return Ok(session);
        }

        // 🔒 POST: api/sessions/join
        [HttpPost("join-jam")]
        public IActionResult Join([FromBody] JoinModel request)
        {
            var sessions = _store.LoadSessions();
            var session = sessions.FirstOrDefault(s => s.Id == request.SessionId);

            if (session == null)
                return NotFound("Session not found");

            if (session.IsPrivate && session.Password != request.Password)
                return BadRequest("Incorrect password");

            return Ok(session);
        }
        [HttpGet("play-audio/{fileName}")]
        public IActionResult PlayAudio(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest();

            // prevent path traversal
            fileName = Path.GetFileName(fileName);

            // wwwroot/audio/<fileName>
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, "audio", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"Not found: {filePath}");

            // serve as mp3 with byte ranges
            return new PhysicalFileResult(filePath, "audio/mpeg")
            {
                EnableRangeProcessing = true
            };
        }

        // DELETE: api/sessions/delete-session/{id}
        [HttpDelete("delete-session/{id}")]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Missing id.");

            var sessions = _store.LoadSessions();
            var toRemove = sessions.FirstOrDefault(s => s.Id == id);
            if (toRemove is null) return NotFound("Session not found.");

            sessions.Remove(toRemove);
            _store.SaveSessions(sessions);

            return NoContent(); // 204
        }





    }
}

