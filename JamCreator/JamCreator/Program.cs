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
using Npgsql.EntityFrameworkCore.PostgreSQL;
using JamCreator.Services;
using JamCreator.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });

builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
builder.Services.AddScoped<IAudioMoodService, AudioMoodService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(
    opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("https://localhost:5191")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
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
app.MapHub<MusicHub>("/musichub");
app.MapHub<ChatHub>("/chathub");

app.Run();
