using Bunit;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Components;
using JamCreator.Client.Pages;

public class CreateJamTests : TestContext
{
    [Fact]
    public void SubmitForm_ValidData_CallsApiAndNavigatesToJoinJam()
    {
       // Arrange: strict expectation for the POST
        var baseUri = new Uri("http://localhost");
        var mockHttp = new MockHttpMessageHandler();

        // Expect exactly one POST to this absolute URL
        mockHttp.Expect(HttpMethod.Post, $"{baseUri}api/sessions/create-jam")
                .Respond("application/json", "{}");

        // Replace any existing HttpClient DI with our mock-backed one
        Services.RemoveAll<HttpClient>();
        Services.AddScoped(_ => new HttpClient(mockHttp) { BaseAddress = baseUri });

        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<CreateJam>();

        // Fill just the room name (enough for your OnSubmit handler)
        cut.Find("input").Change("Jam One");

        // Act
        cut.Find("form").Submit();

        // Assert (wait for async flow to finish)
        cut.WaitForAssertion(() =>
        {
            // Verifies that all expectations were met (i.e., the POST happened)
            mockHttp.VerifyNoOutstandingExpectation();

            // And navigation occurred
            Assert.Equal("/join-jam", new Uri(nav.Uri).AbsolutePath);
        }, timeout: TimeSpan.FromSeconds(3));
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
