using JamCreator.Client.Pages;
using JamCreator.Components;
using JamCreator.Models;
using System.Net.Http;
using Microsoft.AspNetCore.Components;


var builder = WebApplication.CreateBuilder(args);
var sessions = new List<JamSession>();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Basic HttpClient for components rendered via the server
builder.Services.AddHttpClient();

// BaseAddress = current app origin (so you can call "api/..." with a relative URL)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });




var app = builder.Build();

app.MapPost("/api/sessions", HandleCreateSession);
app.MapGet("/api/sessions/{id}", HandleGetSession);

IResult HandleCreateSession(JamCreatorUser jam)
{
    if (string.IsNullOrWhiteSpace(jam.RoomName) || jam.MaxPeople is null)
    {
        var errors = new Dictionary<string, string[]>
        {
            ["RoomName"] = new[] { "RoomName is required." },
            ["PeopleSize"] = new[] { "PeopleSize is required." }
        };
        return Results.ValidationProblem(errors);
    }

    var session = new JamSession
    {
        Id = Guid.NewGuid().ToString("N"),
        RoomName = jam.RoomName!,
        MaxPeople = jam.MaxPeople.Value,
        Genre = jam.Genre,
        Description = jam.Description,
        IsPrivate = jam.IsPrivate,
        Mood = jam.Mood,
        DurationMinutes = jam.DurationMinutes,
        AllowSkipVote = jam.AllowSkipVote
    };

    sessions.Add(session);
    return Results.Created($"/api/sessions/{session.Id}", session);
}

IResult HandleGetSession(string id)
{
    var found = sessions.FirstOrDefault(x => x.Id == id);
    if (found == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(found);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from the server!" }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(JamCreator.Client._Imports).Assembly);


app.Run();
