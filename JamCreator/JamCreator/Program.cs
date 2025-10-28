using System;
using System.Collections.Concurrent;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Components;
using JamCreator.Client;
using System.Text.Json;
using System.Net.Http;
using JamCreator.Services;


var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
builder.Services.AddScoped<FileSessionStore>();
=======
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Basic HttpClient for components rendered via the server
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
// BaseAddress = current app origin (so you can call "api/..." with a relative URL)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });
>>>>>>> origin/main

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor(); // Required for accessing HttpContext

        // Enable CORS to allow API calls
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins("https://localhost:5191") // Blazor app URL
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
var app = builder.Build();

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.UseCors();
app.UseRouting();

<<<<<<< HEAD
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

=======
app.MapHub<ChatHub>("/chathub");
>>>>>>> origin/main
app.Run();
