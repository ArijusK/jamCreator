using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JamCreator.Data;
using JamCreator.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ChatControllerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetHistory_MultipleMessages_ReturnsAscendingByTime()
    {
        using var ctx = CreateContext();

        // older message
        ctx.ChatMessages.Add(new JamCreator.Shared.Models.ChatMessage
        {
            Id = 1,
            User = "A",
            TExt = "First",
            Avatar = "a.png",
            SentAtUtc = DateTime.UtcNow.AddMinutes(-10)
        });

        // newer message
        ctx.ChatMessages.Add(new JamCreator.Shared.Models.ChatMessage
        {
            Id = 2,
            User = "B",
            TExt = "Second",
            Avatar = "b.png",
            SentAtUtc = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        var controller = new ChatController(ctx);

        var result = await controller.GetHistory(); // default take

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ChatMessageDto>>(ok.Value);

        var list = items.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("First", list[0].Text);   // older first
        Assert.Equal("Second", list[1].Text);  // newer last
    }

    [Fact]
    public async Task GetHistory_TakeIsZero_ClampedToOne_ReturnsSingleNewestMessage()
    {
        using var ctx = CreateContext();

        ctx.ChatMessages.Add(new JamCreator.Shared.Models.ChatMessage
        {
            Id = 1,
            User = "A",
            TExt = "Old",
            Avatar = "a.png",
            SentAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        ctx.ChatMessages.Add(new JamCreator.Shared.Models.ChatMessage
        {
            Id = 2,
            User = "B",
            TExt = "New",
            Avatar = "b.png",
            SentAtUtc = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        var controller = new ChatController(ctx);

        var result = await controller.GetHistory(0); // will be clamped to 1

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ChatMessageDto>>(ok.Value);
        var list = items.ToList();

        Assert.Single(list);
        Assert.Equal("New", list[0].Text); // newest only
    }

    [Fact]
    public async Task GetHistory_TakeTooLarge_ClampedToMax_ReturnsAtMost200()
    {
        using var ctx = CreateContext();

        for (int i = 0; i < 250; i++)
        {
            ctx.ChatMessages.Add(new JamCreator.Shared.Models.ChatMessage
            {
                Id = i + 1,
                User = "User",
                TExt = $"Msg {i}",
                Avatar = "x.png",
                SentAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await ctx.SaveChangesAsync();

        var controller = new ChatController(ctx);

        var result = await controller.GetHistory(1000); // will be clamped to 200

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ChatMessageDto>>(ok.Value);
        var list = items.ToList();

        Assert.Equal(200, list.Count);
    }
}
