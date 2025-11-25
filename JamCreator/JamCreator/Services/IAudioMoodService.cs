using JamCreator.Shared.Models;

namespace JamCreator.Services
{
    public interface IAudioMoodService
    {
        Task AssignTracksAsync(JamSessionModel session, CancellationToken ct);
    }
}
