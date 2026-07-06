using ERPSystem.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<DevAuthInitializer>();
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5218";
    return new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
});

var host = builder.Build();

var devAuth = host.Services.GetRequiredService<DevAuthInitializer>();
await devAuth.InitializeAsync();

await host.RunAsync();
