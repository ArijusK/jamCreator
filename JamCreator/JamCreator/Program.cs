using System;
using System.Collections.Concurrent;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Components;
using JamCreator.Client;
using JamCreator.Client.Services;


var builder = WebApplication.CreateBuilder(args);

// Razor + WebAssembly
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConcurrentDictionary<string, JamSessionModel>>();
builder.Services.AddScoped<JamSessionService>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();


//builder.Services.AddHttpClient<JamSessionService>(client =>
//{
//  client.BaseAddress = new Uri("https://localhost:5001"); // <-- your server URL
//});

var app = builder.Build();

//var sessions = app.Services.GetRequiredService<ConcurrentDictionary<string, JamSessionModel>>();

// In-memory storage
var sessions = new List<JamSessionModel>();

// Create a session
app.MapPost("/api/sessions", (JamCreatorUser jam) =>
{
    var session = new JamSessionModel
    {
        Id = Guid.NewGuid().ToString("N"),
        RoomName = jam.RoomName,
        MaxPeople = jam.MaxPeople ?? 4,
        Genre = jam.Genre,
        Mood = jam.Mood,
        Description = jam.Description,
        IsPrivate = jam.IsPrivate,
        Password = jam.Password,
        DurationMinutes = jam.DurationMinutes,
        AllowSkipVote = jam.AllowSkipVote
    };

    sessions.Add(session);
    return Results.Created($"/api/sessions/{session.Id}", session);
});

// Get all sessions
app.MapGet("/api/sessions", () => Results.Ok(sessions));


// Get session by ID
app.MapGet("/api/sessions/{id}", (string id, string? password) =>
{
    var session = sessions.FirstOrDefault(s => s.Id == id);
    if (session == null) return Results.NotFound();
    if (session.IsPrivate && session.Password != password) return Results.BadRequest("Invalid password");
    return Results.Ok(session);
});

// Join session
app.MapPost("/api/sessions/join", (JoinModel join) =>
{
    var session = sessions.FirstOrDefault(s => s.Id == join.SessionId);
    if (session == null) return Results.NotFound();
    if (session.IsPrivate && session.Password != join.Password) return Results.BadRequest("Invalid password");
    return Results.Ok(session);
});

app.UseWebAssemblyDebugging();
app.UseHttpsRedirection();
app.UseAntiforgery(); 
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode();
app.Run();
