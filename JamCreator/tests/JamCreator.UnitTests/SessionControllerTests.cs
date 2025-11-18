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
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.GetById("", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {

        using var ctx = CreateContext();
        var controller = CreateController(ctx);


        var result = await controller.GetById("unknown-id", CancellationToken.None);


        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ExistingSession_ReturnsOkWithDto()
    {

        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var session = new JamSessionModel
        {
            Id = "session-1",
            RoomName = "My Jam",

        };

        ctx.JamSessions.Add(session);
        await ctx.SaveChangesAsync();

        var actionResult = await controller.GetById("session-1", CancellationToken.None);


        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<JamSessionDto>(ok.Value);

        Assert.Equal("session-1", dto.Id);
        Assert.Equal("My Jam", dto.RoomName);
    }
}

/* com */
