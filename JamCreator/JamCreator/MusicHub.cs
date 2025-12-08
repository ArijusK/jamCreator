using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class MusicHub : Hub
{
    private class PlaybackState
    {
        public string? TrackKey { get; set; }
        public double PositionSeconds { get; set; }
        public bool IsPlaying { get; set; }

        public DateTime LastUpdateUtc { get; set; }
    }
    private static readonly ConcurrentDictionary<string, PlaybackState> _playbackStates
        = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, HashSet<string>>> _sessionVotes
        = new();

    public Task JoinSessionGroup(string sessionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public Task LeaveSessionGroup(string sessionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

    public Task BroadcastPlayTrack(string sessionId, string trackKey, double positionSeconds)
    {
        var state = _playbackStates.GetOrAdd(sessionId, _ => new PlaybackState());
        state.TrackKey = trackKey;
        state.PositionSeconds = positionSeconds;
        state.IsPlaying = true;
        state.LastUpdateUtc = DateTime.UtcNow;

        return Clients
            .OthersInGroup(sessionId)
            .SendAsync("PlayTrack", trackKey, positionSeconds);
    }

    public Task BroadcastPauseTrack(string sessionId, string trackKey, double positionSeconds)
    {
        if (_playbackStates.TryGetValue(sessionId, out var state))
        {
            state.TrackKey = trackKey;
            state.PositionSeconds = positionSeconds;
            state.IsPlaying = false;
            state.LastUpdateUtc = DateTime.UtcNow;
        }

        return Clients
            .OthersInGroup(sessionId)
            .SendAsync("PauseTrack", trackKey, positionSeconds);
    }

    public Task RequestCurrentPlayback(string sessionId)
    {
        if (_playbackStates.TryGetValue(sessionId, out var state)
            && !string.IsNullOrEmpty(state.TrackKey))
        {
            double currentPos = state.PositionSeconds;

            if (state.IsPlaying)
            {
                var elapsed = (DateTime.UtcNow - state.LastUpdateUtc).TotalSeconds;
                if (elapsed > 0)
                {
                    currentPos += elapsed;
                }
            }

            return Clients.Caller.SendAsync(
                "CurrentPlayback",
                state.TrackKey,
                currentPos,
                state.IsPlaying);
        }

        return Clients.Caller.SendAsync(
            "CurrentPlayback",
            null,
            0.0,
            false);
    }

    public Task RequestVotes(string sessionId)
    {
        var (counts, winner) = GetVoteSnapshot(sessionId);
        return Clients.Caller.SendAsync("VotesUpdated", counts, winner);
    }

    public Task VoteForTrack(string sessionId, int trackId, string voterId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(voterId))
            return Task.CompletedTask;

        var sessionDict = _sessionVotes.GetOrAdd(sessionId, _ => new ConcurrentDictionary<int, HashSet<string>>());

        foreach (var kvp in sessionDict)
        {
            lock (kvp.Value)
            {
                kvp.Value.Remove(voterId);
            }
        }

        var voters = sessionDict.GetOrAdd(trackId, _ => new HashSet<string>());
        lock (voters)
        {
            voters.Add(voterId);
        }

        var (counts, winner) = GetVoteSnapshot(sessionId);

        return Clients.Group(sessionId).SendAsync("VotesUpdated", counts, winner);
    }

    public Task ClearVotes(string sessionId)
    {
        _sessionVotes.TryRemove(sessionId, out _);
        return Clients.Group(sessionId).SendAsync("VotesUpdated", new Dictionary<int, int>(), null);
    }

    private static (Dictionary<int, int> counts, int? winner) GetVoteSnapshot(string sessionId)
    {
        if (!_sessionVotes.TryGetValue(sessionId, out var sessionDict) || sessionDict.IsEmpty)
        {
            return (new Dictionary<int, int>(), null);
        }

        var counts = new Dictionary<int, int>();
        foreach (var kvp in sessionDict)
        {
            lock (kvp.Value)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
        }

        if (counts.Count == 0)
            return (counts, null);

        var winner = counts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .First().Key;

        return (counts, winner);
    }
    public Task RemoveVotesForVoter(string sessionId, string voterId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(voterId))
            return Task.CompletedTask;

        if (!_sessionVotes.TryGetValue(sessionId, out var sessionDict) || sessionDict.IsEmpty)
            return Task.CompletedTask;

        foreach (var kvp in sessionDict)
        {
            var voters = kvp.Value;
            lock (voters)
            {
                voters.Remove(voterId);
            }
        }

        foreach (var kvp in sessionDict.ToList())
        {
            if (kvp.Value.Count == 0)
            {
                sessionDict.TryRemove(kvp.Key, out _);
            }
        }

        var (counts, winner) = GetVoteSnapshot(sessionId);
        return Clients.Group(sessionId).SendAsync("VotesUpdated", counts, winner);
    }
    public Task ClearVotesForTrack(string sessionId, int trackId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        if (!_sessionVotes.TryGetValue(sessionId, out var sessionDict) || sessionDict.IsEmpty)
            return Task.CompletedTask;
            
        sessionDict.TryRemove(trackId, out _);

        var (counts, winner) = GetVoteSnapshot(sessionId);
        return Clients.Group(sessionId).SendAsync("VotesUpdated", counts, winner);
    }

}
