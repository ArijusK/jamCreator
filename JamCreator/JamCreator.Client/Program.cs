using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JamCreator.Client;
using JamCreator.Client.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped<JamSessionService>();
builder.RootComponents.Add<App>("#app");

// Set base address to the server URL
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});


await builder.Build().RunAsync();