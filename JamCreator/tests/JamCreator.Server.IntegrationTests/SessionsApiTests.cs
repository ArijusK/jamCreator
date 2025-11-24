using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JamCreator.Server.IntegrationTests;

public class SessionsApiTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public SessionsApiTests()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [Fact]
    public async Task GetSessions_EmptyDatabase_ReturnsEmptyArray()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/sessions/get-sessions");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content.ReadFromJsonAsync<JamSessionModel[]>();
        Assert.NotNull(sessions);
        Assert.Empty(sessions!);
    }

    [Fact]
    public async Task CreateSession_ThenGetSessions_ReturnsCreatedSession()
    {
        var client = _factory.CreateClient();

        var newSession = new JamSessionModel
        {
            RoomName = "Integration Test Jam",
            IsPrivate = false
        };

        // Create session via real HTTP POST
        var createResponse = await client.PostAsJsonAsync("/api/sessions/create-jam", newSession);
        createResponse.EnsureSuccessStatusCode();

        // Fetch sessions via real HTTP GET
        var listResponse = await client.GetAsync("/api/sessions/get-sessions");
        listResponse.EnsureSuccessStatusCode();

        var sessions = await listResponse.Content.ReadFromJsonAsync<JamSessionModel[]>();

        Assert.NotNull(sessions);
        Assert.Contains(sessions!, s => s.RoomName == "Integration Test Jam");
    }
}
