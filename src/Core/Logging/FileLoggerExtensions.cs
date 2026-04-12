using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebTemplate.Core.Logging;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddDetailedFileLogging(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        string applicationName)
    {
        var storageRoot = Path.GetFullPath(configuration.GetValue<string>("Storage:RootPath") ?? "storage");
        var logDirectory = Path.Combine(storageRoot, "logs");
        var minimumLevel = configuration.GetValue("Logging:File:MinimumLevel", LogLevel.Information);

        builder.Services.AddSingleton<ILoggerProvider>(_ =>
            new RollingFileLoggerProvider(logDirectory, applicationName, minimumLevel));

        return builder;
    }
}
