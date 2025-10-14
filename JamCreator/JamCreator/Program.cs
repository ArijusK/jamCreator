using JamCreator.Client.Pages;
using JamCreator.Components;

using System.Net.Http;
using Microsoft.AspNetCore.Components;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Basic HttpClient for components rendered via the server
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
// BaseAddress = current app origin (so you can call "api/..." with a relative URL)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });


var app = builder.Build();



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

app.MapHub<ChatHub>("/chathub");
app.Run();
