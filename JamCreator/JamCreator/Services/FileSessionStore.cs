using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using JamCreator.Shared.Models;

namespace JamCreator.Services
{
    public class FileSessionStore
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public FileSessionStore(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "sessions.json");
        }

        // Helper: load sessions from file
        public List<JamSessionModel> LoadSessions()
        {
            if (!System.IO.File.Exists(_filePath))
            {
                Console.WriteLine("[FileSessionStore] No sessions.json found.");
                return new List<JamSessionModel>();
            }

            try
            {
                var json = System.IO.File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<JamSessionModel>>(json) ?? new();
            }
            catch (Exception ex) 
            {
                throw new SessionStoreException("Failed to read sessions.json", ex);
            }
        }

        // Helper: save sessions to file
        public void SaveSessions(List<JamSessionModel> sessions)
        {
            try
            {
                var json = JsonSerializer.Serialize(sessions, _jsonOptions);
                Console.WriteLine($"[SaveSessions] Writing to {_filePath}");
                System.IO.File.WriteAllText(_filePath, json);
            }
            catch (Exception ex) 
            {
                throw new SessionStoreException("Failed to write sessions.json", ex);
            }
        }
    }
}
