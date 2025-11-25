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

    [Fact]
    public async Task Create_ValidModel_ReturnsCreatedAndCallsRepository()
    {
        // Arrange
        using var ctx = CreateContext();

        var sessionsRepoMock = new Mock<IRepository<JamSessionModel, string>>();
        var participantsRepoMock = new Mock<IRepository<SessionParticipant, int>>();
        var tracksRepoMock = new Mock<IRepository<AudioTrack, int>>();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        JamSessionModel? capturedSession = null;

        sessionsRepoMock
            .Setup(r => r.AddAsync(It.IsAny<JamSessionModel>(), It.IsAny<CancellationToken>()))
            .Callback<JamSessionModel, CancellationToken>((s, _) => capturedSession = s)
            .Returns(Task.CompletedTask);

        var controller = new SessionsController(
            sessionsRepoMock.Object,
            participantsRepoMock.Object,
            tracksRepoMock.Object,
            ctx,
            envMock.Object);

        var model = new JamCreateModel
        {
            RoomName = "Unit Test Room",
            Genre = "Rock",
            Description = "Test description",
            IsPrivate = false,
            Password = null,
            Mood = JamMood.Chill,
            MaxPeople = 5,
            DurationMinutes = 60,
            AllowSkipVote = true
        };

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(capturedSession);

        // repository received the right data
        Assert.Equal("Unit Test Room", capturedSession!.RoomName);
        Assert.Equal("Rock", capturedSession.Genre);
        Assert.Equal(JamMood.Chill, capturedSession.Mood);
        Assert.Equal(5, capturedSession.MaxPeople);

        // location and value use the generated Id
        Assert.Equal($"/api/sessions/get-session-id/{capturedSession.Id}", created.Location);
        Assert.Equal(capturedSession.Id, created.Value);
    }
    
    [Fact]
    public async Task Create_EmptyRoomName_ReturnsBadRequest()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var model = new JamCreateModel
        {
            RoomName = "",   // invalid
            Genre = "Rock"
        };

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid session data", badRequest.Value);
    }

    [Fact]
    public async Task GetAll_NoSessions_ReturnsEmptyList()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        // Act
        var actionResult = await controller.GetAll(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var list = Assert.IsType<List<JamSessionDto>>(ok.Value);

        Assert.Empty(list);
    }

        [Fact]
    public async Task GetAll_WithSessions_ReturnsOrderedDtos()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var older = new JamSessionModel
        {
            Id = "session-1",
            RoomName = "Older",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        var newer = new JamSessionModel
        {
            Id = "session-2",
            RoomName = "Newer",
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.JamSessions.AddRange(older, newer);
        await ctx.SaveChangesAsync();

        // Act
        var actionResult = await controller.GetAll(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var list = Assert.IsType<List<JamSessionDto>>(ok.Value);

        Assert.Equal(2, list.Count);
        // should be ordered by CreatedAtUtc descending
        Assert.Equal("Newer", list[0].RoomName);
        Assert.Equal("Older", list[1].RoomName);
    }

    [Fact]
    public async Task Delete_IdIsEmpty_ReturnBadRequest()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Delete("", CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing id.", bad.Value);
    }

    [Fact]
    public async Task Delete_SessionDoesNotExist_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Delete("abc", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Session not found.", notFound.Value);
    }

    [Fact]
    public async Task Delete_ExistingSession_ReturnsNoContent()
    {
        using var ctx = CreateContext();

        // Arrange: we still add to EF (optional)
        ctx.JamSessions.Add(new JamSessionModel
        {
            Id = "session-1",
            RoomName = "Older",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        await ctx.SaveChangesAsync();

        // We can't access the mock via CreateController,
        // so we create the controller MANUALLY for this test only.

        var sessionsRepoMock = new Mock<IRepository<JamSessionModel, string>>();
        sessionsRepoMock
            .Setup(r => r.DeleteByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var participantsRepoMock = new Mock<IRepository<SessionParticipant, int>>();
        var tracksRepoMock = new Mock<IRepository<AudioTrack, int>>();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        var controller = new SessionsController(
            sessionsRepoMock.Object,
            participantsRepoMock.Object,
            tracksRepoMock.Object,
            ctx,
            envMock.Object);

        // Act
        var result = await controller.Delete("session-1", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }



    [Fact]
    public async Task Join_NullRequest_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Join(null, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid join request.", bad.Value);
    }

    [Fact]
    public async Task Join_MissingSessionId_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var request = new JoinModel
        {
            SessionId = "",    // invalid
            DisplayName = "User1"
        };

        var result = await controller.Join(request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid join request.", bad.Value);
    }

    [Fact]
    public async Task Join_UnknownSession_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var request = new JoinModel
        {
            SessionId = "does-not-exist",
            DisplayName = "User1"
        };

        var result = await controller.Join(request, CancellationToken.None);

        var nf = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Session not found.", nf.Value);
    }

    [Fact]
    public void PlayAudio_FileNameIsEmpty_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = controller.PlayAudio("");

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public void PlayAudio_FileDoesNotExist_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var controller = CreateController(ctx);

        var result = controller.PlayAudio("does-not-exist.mp3");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.StartsWith("Not found:", message);
    }

    [Fact]
    public void PlayAudio_FileExists_ReturnsPhysicalFile()
    {
        using var ctx = CreateContext();

        // === 1. Arrange: create the folder and file ===

        // Controller will use: <current folder>/wwwroot/audio
        var root = Directory.GetCurrentDirectory();
        var audioDir = Path.Combine(root, "wwwroot", "audio");
        Directory.CreateDirectory(audioDir);

        var fileName = "test-audio.mp3";
        var fullPath = Path.Combine(audioDir, fileName);

        // Create a dummy file
        File.WriteAllText(fullPath, "dummy");

        // === 2. Act ===
        var controller = CreateController(ctx);
        var result = controller.PlayAudio(fileName);

        // === 3. Assert ===
        var fileResult = Assert.IsType<PhysicalFileResult>(result);

        Assert.Equal("audio/mpeg", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(fullPath, fileResult.FileName);
    }



}

/* com */