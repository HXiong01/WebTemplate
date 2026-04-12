using Microsoft.Extensions.Options;
using WebTemplate.Core.Configuration;
using WebTemplate.Core.Logging;
using WebTemplate.Core.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<GreetingService>();
builder.Services.AddSingleton<HealthSnapshotBuilder>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDetailedFileLogging(builder.Configuration, "api");

var app = builder.Build();

var storageOptions = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
var storageRoot = DirectoryBootstrapper.EnsureStorageDirectories(storageOptions.RootPath);
app.Logger.LogInformation("API storage ready at {StorageRoot}", storageRoot);

app.MapGet("/", () => Results.Redirect("/health"));

app.MapGet("/health", async context =>
{
    var healthBuilder = context.RequestServices.GetRequiredService<HealthSnapshotBuilder>();
    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var snapshot = healthBuilder.Build("api", environment.EnvironmentName);

    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, snapshot);
});

app.MapGet("/api/greeting", async context =>
{
    var greetingService = context.RequestServices.GetRequiredService<GreetingService>();
    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("GreetingEndpoint");
    var name = context.Request.Query["name"].ToString();
    var message = greetingService.CreateGreeting(name);
    logger.LogInformation("Greeting generated for {Name}", string.IsNullOrWhiteSpace(name) ? "Developer" : name.Trim());

    context.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(context.Response.Body, new { message });
});

app.Run();
