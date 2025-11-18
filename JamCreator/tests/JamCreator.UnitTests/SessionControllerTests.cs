using JamCreator.Controllers;
using JamCreator.Data;
using JamCreator.Shared.Interfaces;
using JamCreator.Shared.Models;
using JamCreator.Shared.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace JamCreator.UnitTests;

public class SessionsControllerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static SessionsController CreateController(AppDbContext ctx)
    {
        // Repositories are not used by GetById, so simple mocks are enough here.
        var sessionsRepoMock = new Mock<IRepository<JamSessionModel, string>>();
        var participantsRepoMock = new Mock<IRepository<SessionParticipant, int>>();
        var tracksRepoMock = new Mock<IRepository<AudioTrack, int>>();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        return new SessionsController(
            sessionsRepoMock.Object,
            participantsRepoMock.Object,
            tracksRepoMock.Object,
            ctx,
            envMock.Object);
    }

    [Fact]
    public async Task GetById_EmptyId_ReturnsBadRequest()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        // Act
        var result = await controller.GetById("", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        // Act
        var result = await controller.GetById("unknown-id", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ExistingSession_ReturnsOkWithDto()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var session = new JamSessionModel
        {
            Id = "session-1",
            RoomName = "My Jam",
            HostUserId = "host-123"
        };

        ctx.JamSessions.Add(session);
        await ctx.SaveChangesAsync();

        // Act
        var actionResult = await controller.GetById("session-1", CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<JamSessionDto>(ok.Value);

        Assert.Equal("session-1", dto.Id);
        Assert.Equal("My Jam", dto.RoomName);
    }
}

/* com */
