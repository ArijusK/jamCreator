using JamCreator.Data;
using JamCreator.Services;
using JamCreator.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

public class AudioMoodServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AssignTracksAsync_WhenMp3FilesExist_AddsTracksToDatabase()
    {
        using var ctx = CreateContext();

        // --- Arrange filesystem ---
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var webRoot = Path.Combine(root, "wwwroot");
        var moodFolder = Path.Combine(webRoot, "audio", "chill");

        Directory.CreateDirectory(moodFolder);

        var fakeMp3 = Path.Combine(moodFolder, "test-track.mp3");
        File.WriteAllText(fakeMp3, "fake audio");

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.WebRootPath).Returns(webRoot);
        envMock.SetupGet(e => e.ContentRootPath).Returns(root);

        var sut = new AudioMoodService(ctx, envMock.Object);

        var session = new JamSessionModel
        {
            Id   = "session-1",
            Mood = JamMood.Chill
        };

        // --- Act ---
        await sut.AssignTracksAsync(session, CancellationToken.None);

        // --- Assert ---
        var tracks = await ctx.Tracks.ToListAsync();
        Assert.Single(tracks);

        var track = tracks[0];
        Assert.Equal("session-1", track.JamSessionId);
        Assert.Equal("test-track.mp3", track.FileName);
        Assert.Equal("test-track", track.Title);
        Assert.Equal(JamMood.Chill, track.Mood);

        // --- Cleanup ---
        Directory.Delete(root, recursive: true);
    }
}
