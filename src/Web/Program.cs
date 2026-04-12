using Microsoft.Extensions.Options;
using WebTemplate.Core.Configuration;
using WebTemplate.Core.Logging;
using WebTemplate.Core.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<WebClientOptions>(builder.Configuration.GetSection("WebClient"));
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<HealthSnapshotBuilder>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDetailedFileLogging(builder.Configuration, "web");

var app = builder.Build();

var storageOptions = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
var webClientOptions = app.Services.GetRequiredService<IOptions<WebClientOptions>>().Value;
var storageRoot = DirectoryBootstrapper.EnsureStorageDirectories(storageOptions.RootPath);
app.Logger.LogInformation("Web storage ready at {StorageRoot}", storageRoot);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async context =>
{
    var healthBuilder = context.RequestServices.GetRequiredService<HealthSnapshotBuilder>();
    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var snapshot = healthBuilder.Build("web", environment.EnvironmentName);

    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, snapshot);
});

app.MapGet("/config.js", async context =>
{
    context.Response.ContentType = "application/javascript";
    await context.Response.WriteAsync($"window.WEB_TEMPLATE_CONFIG = {{ apiBaseUrl: '{webClientOptions.ApiBaseUrl}' }};");
});

app.Run();
