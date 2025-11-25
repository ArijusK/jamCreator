using System;
using System.Threading;
using System.Threading.Tasks;
using JamCreator.Data;
using JamCreator.Shared.Models;
using JamCreator.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JamCreator.UnitTests;

public class ProfileControllerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Get_ProfileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = new ProfileController(ctx);

        // Act
        var result = await controller.Get("user-1", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_ProfileExists_ReturnsUserProfileDto()
    {
        // Arrange
        using var ctx = CreateContext();
        var profile = new UserProfile
        {
            Id = "user-1",
            Username = "Alice",
            FavoriteGenre = "Rock",
            Avatar = "🎧",
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var controller = new ProfileController(ctx);

        // Act
        var actionResult = await controller.Get("user-1", CancellationToken.None);

        // Assert
        Assert.Null(actionResult.Result); // direct DTO, no ActionResult wrapper
        var dto = Assert.IsType<UserProfileDto>(actionResult.Value);

        Assert.Equal("user-1", dto.Id);
        Assert.Equal("Alice", dto.Username);
        Assert.Equal("Rock", dto.FavoriteGenre);
        Assert.Equal("🎧", dto.Avatar);
    }

    [Fact]
    public async Task Upsert_ProfileDoesNotExist_CreatesProfileAndReturnsDto()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = new ProfileController(ctx);

        var dto = new UserProfileDto
        {
            Username = "  Bob  ",          // check trimming
            FavoriteGenre = "  Jazz ",
            Avatar = ""                  // should become default 🎸
        };

        // Act
        var result = await controller.Upsert("user-2", dto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<UserProfileDto>(ok.Value);

        Assert.Equal("user-2", returned.Id);
        Assert.Equal("Bob", returned.Username);              // trimmed
        Assert.Equal("Jazz", returned.FavoriteGenre);        // trimmed
        Assert.Equal("🎸", returned.Avatar);                 // default avatar

        // verify persisted entity
        var entity = await ctx.UserProfiles.FirstOrDefaultAsync(p => p.Id == "user-2");
        Assert.NotNull(entity);
        Assert.Equal("Bob", entity!.Username);
        Assert.Equal("Jazz", entity.FavoriteGenre);
        Assert.Equal("🎸", entity.Avatar);
    }

    [Fact]
    public async Task Upsert_ProfileExists_UpdatesProfileAndReturnsDto()
    {
        // Arrange
        using var ctx = CreateContext();
        var existing = new UserProfile
        {
            Id = "user-3",
            Username = "OldName",
            FavoriteGenre = "OldGenre",
            Avatar = "🙂",
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        ctx.UserProfiles.Add(existing);
        await ctx.SaveChangesAsync();

        var originalUpdatedAt = existing.UpdatedAtUtc;

        var controller = new ProfileController(ctx);

        var dto = new UserProfileDto
        {
            Username = "NewName",
            FavoriteGenre = "Metal",
            Avatar = "🤘"
        };

        // Act
        var result = await controller.Upsert("user-3", dto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<UserProfileDto>(ok.Value);

        Assert.Equal("user-3", returned.Id);
        Assert.Equal("NewName", returned.Username);
        Assert.Equal("Metal", returned.FavoriteGenre);
        Assert.Equal("🤘", returned.Avatar);

        var entity = await ctx.UserProfiles.FirstAsync(p => p.Id == "user-3");
        Assert.Equal("NewName", entity.Username);
        Assert.Equal("Metal", entity.FavoriteGenre);
        Assert.Equal("🤘", entity.Avatar);
        Assert.True(entity.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public async Task Delete_ProfileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var ctx = CreateContext();
        var controller = new ProfileController(ctx);

        // Act
        var result = await controller.Delete("missing-user", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ProfileExists_RemovesProfileAndReturnsNoContent()
    {
        // Arrange
        using var ctx = CreateContext();
        var profile = new UserProfile
        {
            Id = "user-4",
            Username = "ToDelete"
        };
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var controller = new ProfileController(ctx);

        // Act
        var result = await controller.Delete("user-4", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var entity = await ctx.UserProfiles.FirstOrDefaultAsync(p => p.Id == "user-4");
        Assert.Null(entity);
    }
}