using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

public class MusicHubTests
{
    private static MusicHub CreateHub(
        out Mock<IHubCallerClients> clientsMock,
        out Mock<IGroupManager> groupsMock,
        out Mock<HubCallerContext> contextMock,
        out Mock<ISingleClientProxy> callerProxyMock,
        out Mock<IClientProxy> groupProxyMock,
        out Mock<IClientProxy> othersInGroupProxyMock,
        string connectionId = "conn-1")
    {
        clientsMock = new Mock<IHubCallerClients>();
        groupsMock = new Mock<IGroupManager>();
        contextMock = new Mock<HubCallerContext>();

        callerProxyMock = new Mock<ISingleClientProxy>();
        groupProxyMock = new Mock<IClientProxy>();
        othersInGroupProxyMock = new Mock<IClientProxy>();

        contextMock.SetupGet(c => c.ConnectionId).Returns(connectionId);

        clientsMock.SetupGet(c => c.Caller).Returns(callerProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxyMock.Object);
        clientsMock.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(othersInGroupProxyMock.Object);

        // Make SendCoreAsync always "work" so hub methods don't throw.
        callerProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        groupProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        othersInGroupProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new MusicHub
        {
            Clients = clientsMock.Object,
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };
    }

    [Fact]
    public async Task JoinSessionGroup_CallsAddToGroupAsync()
    {
        var hub = CreateHub(out _, out var groupsMock, out _, out _, out _, out _, "c1");

        await hub.JoinSessionGroup("session-1");

        groupsMock.Verify(g => g.AddToGroupAsync("c1", "session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveSessionGroup_CallsRemoveFromGroupAsync()
    {
        var hub = CreateHub(out _, out var groupsMock, out _, out _, out _, out _, "c1");

        await hub.LeaveSessionGroup("session-1");

        groupsMock.Verify(g => g.RemoveFromGroupAsync("c1", "session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastPlayTrack_SendsPlayTrackToOthersInGroup()
    {
        var hub = CreateHub(out _, out _, out _, out _, out _, out var othersProxy);

        await hub.BroadcastPlayTrack("session-1", "trackA", 12.5);

        othersProxy.Verify(p => p.SendCoreAsync(
                "PlayTrack",
                It.Is<object[]>(args =>
                    (string)args[0] == "trackA" &&
                    (double)args[1] == 12.5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastPauseTrack_WithoutExistingState_StillBroadcastsPause()
    {
        var hub = CreateHub(out _, out _, out _, out _, out _, out var othersProxy);

        await hub.BroadcastPauseTrack("no-state-session", "trackA", 99);

        othersProxy.Verify(p => p.SendCoreAsync(
                "PauseTrack",
                It.Is<object[]>(args =>
                    (string)args[0] == "trackA" &&
                    (double)args[1] == 99),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestCurrentPlayback_NoState_SendsNullPlayback()
    {
        var hub = CreateHub(out _, out _, out _, out var callerProxy, out _, out _);

        await hub.RequestCurrentPlayback("unknown-session");

        callerProxy.Verify(p => p.SendCoreAsync(
                "CurrentPlayback",
                It.Is<object[]>(args =>
                    args[0] == null &&
                    (double)args[1] == 0.0 &&
                    (bool)args[2] == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestCurrentPlayback_WithState_SendsTrackKeyAndPosition()
    {
        var hub = CreateHub(out _, out _, out _, out var callerProxy, out _, out _);

        await hub.BroadcastPlayTrack("session-55", "trackZ", 5.0);
        await hub.RequestCurrentPlayback("session-55");

        callerProxy.Verify(p => p.SendCoreAsync(
                "CurrentPlayback",
                It.Is<object[]>(args =>
                    (string)args[0] == "trackZ" &&
                    (double)args[1] >= 5.0 &&
                    (bool)args[2] == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VoteForTrack_InvalidInputs_DoesNothing()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        await hub.VoteForTrack("", 1, "voter");
        await hub.VoteForTrack("s", 1, "");
        await hub.VoteForTrack("s", 1, "   ");

        groupProxy.Verify(
            p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VoteForTrack_FirstVote_BroadcastsCountsAndWinner()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        object[]? lastArgs = null;
        groupProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-1", 10, "alice");

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];
        var winner = (int?)lastArgs![1];

        Assert.Equal(1, counts[10]);
        Assert.Equal(10, winner);
    }

    [Fact]
    public async Task VoteForTrack_SameVoterMovesVote_RemovesOldAndSetsNewWinner()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        object[]? lastArgs = null;
        groupProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-1", 10, "alice");
        await hub.VoteForTrack("session-1", 20, "alice"); // move vote

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];
        var winner = (int?)lastArgs![1];

        // Track 20 should now have 1 vote
        Assert.True(counts.ContainsKey(20));
        Assert.Equal(1, counts[20]);

        // Track 10 may still exist with 0 votes (implementation detail)
        Assert.True(counts.ContainsKey(10));
        Assert.Equal(0, counts[10]);

        Assert.Equal(20, winner);

    }

    [Fact]
    public async Task RequestVotes_ReturnsSnapshotToCaller()
    {
        var hub = CreateHub(out _, out _, out _, out var callerProxy, out _, out _);

        object[]? lastArgs = null;
        callerProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-2", 10, "a");
        await hub.VoteForTrack("session-2", 10, "b");
        await hub.VoteForTrack("session-2", 20, "c");

        await hub.RequestVotes("session-2");

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];
        var winner = (int?)lastArgs![1];

        Assert.Equal(2, counts[10]);
        Assert.Equal(1, counts[20]);
        Assert.Equal(10, winner);
    }

    [Fact]
    public async Task ClearVotes_ClearsAndBroadcastsEmpty()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        object[]? lastArgs = null;
        groupProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-3", 10, "a");
        await hub.ClearVotes("session-3");

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];
        var winner = (int?)lastArgs![1];

        Assert.Empty(counts);
        Assert.Null(winner);
    }

    [Fact]
    public async Task RemoveVotesForVoter_RemovesAndBroadcasts()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        object[]? lastArgs = null;
        groupProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-4", 10, "a");
        await hub.VoteForTrack("session-4", 20, "b");

        await hub.RemoveVotesForVoter("session-4", "a");

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];

        Assert.False(counts.ContainsKey(10));
        Assert.Equal(1, counts[20]);
    }

    [Fact]
    public async Task ClearVotesForTrack_RemovesTrackAndBroadcasts()
    {
        var hub = CreateHub(out _, out _, out _, out _, out var groupProxy, out _);

        object[]? lastArgs = null;
        groupProxy
            .Setup(p => p.SendCoreAsync("VotesUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => lastArgs = args)
            .Returns(Task.CompletedTask);

        await hub.VoteForTrack("session-5", 10, "a");
        await hub.VoteForTrack("session-5", 20, "b");

        await hub.ClearVotesForTrack("session-5", 10);

        Assert.NotNull(lastArgs);
        var counts = (Dictionary<int, int>)lastArgs![0];

        Assert.False(counts.ContainsKey(10));
        Assert.Equal(1, counts[20]);
    }
}
