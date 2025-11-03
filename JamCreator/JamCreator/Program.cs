using System;
using System.Collections.Concurrent;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Components;
using JamCreator.Client;
using System.Text.Json;
using System.Net.Http;
using JamCreator.Data;
using Microsoft.EntityFrameworkCore;
using JamCreator.Shared.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
////////////////////////////////////////////////
/// Basic HttpClient for components rendered via the server
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
// BaseAddress = current app origin (so you can call "api/..." with a relative URL)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });
///////////////////////////////////////////////
builder.Services.AddScoped(typeof(JamCreator.Shared.Interfaces.IRepository<,>), typeof(JamCreator.Data.Repository<,>));
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(
    opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

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

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();   // apply migrations automatically during dev
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.UseCors();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chathub");
app.Run();
