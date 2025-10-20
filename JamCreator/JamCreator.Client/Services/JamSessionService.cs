using System.Net.Http;
using System.Net.Http.Json;
using JamCreator.Shared.Models;

namespace JamCreator.Client.Services
{
    public class JamSessionService
    {
        
        private readonly HttpClient _http;
        public JamSessionService(HttpClient http) => _http = http;

        public async Task<JamSessionModel> CreateSessionAsync(JamCreatorUser jam)
        {
            var response = await _http.PostAsJsonAsync("/api/sessions", jam);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JamSessionModel>()!;
        }

        public async Task<List<JamSessionModel>> GetAllSessionsAsync()
        {
            return await _http.GetFromJsonAsync<List<JamSessionModel>>("/api/sessions") ?? new List<JamSessionModel>();
        }

        public async Task<JamSessionModel> JoinSessionAsync(string sessionId, string? password = null)
        {
            var res = await _http.PostAsJsonAsync("/api/sessions/join", new JoinModel
            {
                SessionId = sessionId,
                Password = password
            });

            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<JamSessionModel>()!;
        }
    }
}
