using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WebTemplate.Core.Logging;

public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _applicationName;
    private readonly string _logDirectory;
    private readonly LogLevel _minimumLevel;
    private readonly object _gate = new();
    private bool _disposed;

    public RollingFileLoggerProvider(string logDirectory, string applicationName, LogLevel minimumLevel)
    {
        _logDirectory = logDirectory;
        _applicationName = applicationName;
        _minimumLevel = minimumLevel;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, static (category, provider) =>
            new RollingFileLogger(category, provider), this);
    }

    internal bool IsEnabled(LogLevel logLevel)
    {
        return !_disposed && logLevel != LogLevel.None && logLevel >= _minimumLevel;
    }

    internal void Write(string category, LogLevel level, EventId eventId, Exception? exception, string message)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        var timestamp = DateTimeOffset.Now;
        var filePath = Path.Combine(_logDirectory, $"{_applicationName}-{timestamp:yyyyMMdd}.log");
        var line = $"{timestamp:O} [{level}] {category} ({eventId.Id}) {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_gate)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _loggers.Clear();
    }
}
