using Bunit;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Xunit;
using JamCreator.Client.Pages;
using System.Net.Http;
using System;

public class CreateJamTests : TestContext
{
    [Fact]
    public void SubmitForm_ValidData_CallsApiAndNavigatesToJoinJam()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "http://localhost/api/sessions/create-jam")
                .Respond("application/json", "{}"); // fake success response

        Services.AddSingleton(new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("http://localhost")
        });

        // Track navigation
        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<CreateJam>();

        // Act: find form and submit it

        var input = cut.Find("input");
        input.Change("Jam One");

        var form = cut.Find("form");
        form.Submit();

        // Wait for async
        cut.WaitForAssertion(() =>
        {
            // Assert: verify navigation and API call
            var count = mockHttp.GetMatchCount(
                mockHttp.When(HttpMethod.Post, "http://localhost/api/sessions/create-jam"));
            Assert.Equal(1, count); // POST was made once

            Assert.Equal("/join-jam", new Uri(nav.Uri).AbsolutePath); // navigation happened
        });
    }

    [Fact]
    public void SubmitForm_ValidData_DisplaysGreetingMessage()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "http://localhost/api/sessions/create-jam")
                .Respond("application/json", "{}"); // fake success

        Services.AddSingleton(new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("http://localhost")
        });

        var cut = RenderComponent<CreateJam>();
        
        // Simulate filling out form
        var input = cut.Find("input");
        input.Change("Jazz Session");

        // Act
        var form = cut.Find("form");
        form.Submit();
        // Wait for the greeting to appear
        cut.WaitForAssertion(() =>
        {
            // Assert: GreetingMessage rendered
            var markup = cut.Markup;
            Assert.Contains("Welcome, Jazz Session! Thanks for creating a jam.", markup);
            Assert.Contains("alert-success", markup); // ensure it uses Bootstrap success alert
        });
    }

   [Fact]
    public void RenderPage_WhenLoaded_DisplaysAllFormElements()
    {
        var cut = RenderComponent<CreateJam>();

        // Basic UI presence check
        Assert.Contains("Create Jam Page", cut.Markup);
        Assert.NotNull(cut.Find("input"));
        Assert.NotNull(cut.Find("button"));
        Assert.Contains("Room name", cut.Markup);
        Assert.Contains("Genre", cut.Markup);
        Assert.Contains("Duration", cut.Markup);
    }
}
