using System;
using System.Collections.Concurrent;
using JamCreator.Shared.Models;
using Microsoft.AspNetCore.Components;
using JamCreator.Client;
using System.Text.Json;
using System.Net.Http;
using JamCreator.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<FileSessionStore>();

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

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
