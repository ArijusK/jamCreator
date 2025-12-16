using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JamCreator.Data;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class ChatHubTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ChatHub CreateHub(
        AppDbContext db,
        out Mock<IHubCallerClients> clientsMock,
        out Mock<IGroupManager> groupsMock,
        out Mock<HubCallerContext> contextMock,
        out Mock<IClientProxy> groupProxyMock,
        string connectionId = "conn-1")
    {
        clientsMock = new Mock<IHubCallerClients>();
        groupsMock = new Mock<IGroupManager>();
        contextMock = new Mock<HubCallerContext>();
        groupProxyMock = new Mock<IClientProxy>();

        contextMock.SetupGet(c => c.ConnectionId).Returns(connectionId);

        // IMPORTANT: ChatHub uses Clients.Group(sessionId)
        clientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(groupProxyMock.Object);

        var hub = new ChatHub(db)
        {
            Clients = clientsMock.Object,
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };

        return hub;
    }

    [Fact]
    public async Task JoinSession_CallsAddToGroupAsync()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.JoinSession("session-123");

        groupsMock.Verify(g =>
            g.AddToGroupAsync("conn-1", "session-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_WhitespaceMessage_DoesNotSaveAndDoesNotBroadcast()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.SendMessage("Alice", "   ", "😎", "session-1");

        Assert.Empty(db.ChatMessages);

        groupProxyMock.Verify(p =>
            p.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessage_NullUser_BecomesGuest_AndSaves()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.SendMessage(null!, "Hello", "😎", "session-1");

        var saved = await db.ChatMessages.SingleAsync();
        Assert.Equal("Guest", saved.User);
        Assert.Equal("Hello", saved.Text);
        Assert.Equal("😎", saved.Avatar);
        Assert.Equal("session-1", saved.SessionId);
    }

    [Fact]
    public async Task SendMessage_ValidInput_SavesAndBroadcastsToGroup()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.SendMessage("  Bob  ", "  Yo  ", "  😎  ", "session-777");

        var saved = await db.ChatMessages.SingleAsync();
        Assert.Equal("Bob", saved.User);
        Assert.Equal("Yo", saved.Text);
        Assert.Equal("😎", saved.Avatar);
        Assert.Equal("session-777", saved.SessionId);

        groupProxyMock.Verify(p =>
            p.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args =>
                    args.Length == 4 &&
                    (string)args[0] == "Bob" &&
                    (string)args[1] == "Yo" &&
                    (string)args[2] == "😎" &&
                    args[3] is DateTime),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_DoesNotThrow()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.OnConnectedAsync(); // just cover the override path
    }

    [Fact]
    public async Task OnDisconnectedAsync_DoesNotThrow()
    {
        using var db = CreateContext();

        var hub = CreateHub(db,
            out var clientsMock,
            out var groupsMock,
            out var contextMock,
            out var groupProxyMock);

        await hub.OnDisconnectedAsync(null);
    }
}
