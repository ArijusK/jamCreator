using JamCreator.Data;
using JamCreator.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace JamCreator.UnitTests;

public class RepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsEntity()
    {
        // Arrange
        using var ctx = CreateContext();
        var repo = new Repository<JamSessionModel, string>(ctx);

        var session = new JamSessionModel
        {
            RoomName = "Test Room",
            HostUserId = "host-1"
        };

        // Act
        await repo.AddAsync(session);
        var loaded = await repo.GetByIdAsync(session.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Test Room", loaded!.RoomName);
        Assert.Equal("host-1", loaded.HostUserId);
    }

    [Fact]
    public async Task ListAsync_WithPredicate_FiltersCorrectly()
    {
        // Arrange
        using var ctx = CreateContext();
        var repo = new Repository<JamSessionModel, string>(ctx);

        await repo.AddAsync(new JamSessionModel { RoomName = "Public", IsPrivate = false });
        await repo.AddAsync(new JamSessionModel { RoomName = "Private", IsPrivate = true });

        // Act
        var publicSessions = await repo.ListAsync(s => !s.IsPrivate);

        // Assert
        Assert.Single(publicSessions);
        Assert.Equal("Public", publicSessions[0].RoomName);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntityInDatabase()
    {
        // Arrange
        using var ctx = CreateContext();
        var repo = new Repository<JamSessionModel, string>(ctx);

        var session = new JamSessionModel { RoomName = "Before" };
        await repo.AddAsync(session);

        // Act
        session.RoomName = "After";
        await repo.UpdateAsync(session);
        var loaded = await repo.GetByIdAsync(session.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("After", loaded!.RoomName);
    }

    [Fact]
    public async Task DeleteByIdAsync_RemovesExistingEntityAndReturnsTrue()
    {
        // Arrange
        using var ctx = CreateContext();
        var repo = new Repository<JamSessionModel, string>(ctx);

        var session = new JamSessionModel { RoomName = "To delete" };
        await repo.AddAsync(session);

        // Act
        var result = await repo.DeleteByIdAsync(session.Id);
        var loaded = await repo.GetByIdAsync(session.Id);

        // Assert
        Assert.True(result);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteByIdAsync_ReturnsFalse_WhenEntityDoesNotExist()
    {
        // Arrange
        using var ctx = CreateContext();
        var repo = new Repository<JamSessionModel, string>(ctx);

        // Act
        var result = await repo.DeleteByIdAsync("does-not-exist");

        // Assert
        Assert.False(result);
    }
} /* com */
