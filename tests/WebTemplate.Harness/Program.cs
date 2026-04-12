using Microsoft.Extensions.Logging;
using WebTemplate.Core.Logging;
using WebTemplate.Core.Services;

var runner = new HarnessRunner();
return await runner.RunAsync();

internal sealed class HarnessRunner
{
    private readonly List<(string Name, Func<Task> Test)> _tests;

    public HarnessRunner()
    {
        _tests =
        [
            ("GreetingService returns a trimmed greeting", GreetingService_ReturnsTrimmedGreetingAsync),
            ("GreetingService uses the fallback name", GreetingService_UsesFallbackNameAsync),
            ("HealthSnapshotBuilder uses the clock and environment", HealthSnapshotBuilder_UsesClockAsync),
            ("DirectoryBootstrapper creates the required storage tree", DirectoryBootstrapper_CreatesFoldersAsync),
            ("RollingFileLoggerProvider writes a log file", RollingFileLoggerProvider_WritesFileAsync)
        ];
    }

    public async Task<int> RunAsync()
    {
        Console.WriteLine("Running WebTemplate harness...");

        var failures = new List<string>();
        foreach (var (name, test) in _tests)
        {
            try
            {
                await test();
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"[FAIL] {name}");
            }
        }

        if (failures.Count == 0)
        {
            Console.WriteLine($"All {_tests.Count} harness tests passed.");
            return 0;
        }

        Console.WriteLine("Failures:");
        foreach (var failure in failures)
        {
            Console.WriteLine($" - {failure}");
        }

        return 1;
    }

    private static Task GreetingService_ReturnsTrimmedGreetingAsync()
    {
        var service = new GreetingService();
        var result = service.CreateGreeting("  Casey  ");
        AssertEqual("Hello, Casey. WebTemplate is running.", result);
        return Task.CompletedTask;
    }

    private static Task GreetingService_UsesFallbackNameAsync()
    {
        var service = new GreetingService();
        var result = service.CreateGreeting("   ");
        AssertEqual("Hello, Developer. WebTemplate is running.", result);
        return Task.CompletedTask;
    }

    private static Task HealthSnapshotBuilder_UsesClockAsync()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 04, 12, 14, 30, 0, TimeSpan.Zero));
        var builder = new HealthSnapshotBuilder(clock);

        var snapshot = builder.Build("api", "Development");

        AssertEqual("api", snapshot.Service);
        AssertEqual("Healthy", snapshot.Status);
        AssertEqual(clock.UtcNow, snapshot.CheckedAtUtc);
        AssertEqual("Development", snapshot.Environment);
        return Task.CompletedTask;
    }

    private static Task DirectoryBootstrapper_CreatesFoldersAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "WebTemplateHarness", Guid.NewGuid().ToString("N"));
        try
        {
            var fullRoot = DirectoryBootstrapper.EnsureStorageDirectories(tempRoot);
            AssertTrue(Directory.Exists(fullRoot), "Storage root was not created.");
            AssertTrue(Directory.Exists(Path.Combine(fullRoot, "logs")), "Logs directory was not created.");
            AssertTrue(Directory.Exists(Path.Combine(fullRoot, "artifacts")), "Artifacts directory was not created.");
            return Task.CompletedTask;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static Task RollingFileLoggerProvider_WritesFileAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "WebTemplateHarness", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            using var provider = new RollingFileLoggerProvider(tempRoot, "harness", LogLevel.Information);
            var logger = provider.CreateLogger("Harness");
            logger.LogInformation("Harness log entry");

            var file = Directory.GetFiles(tempRoot, "harness-*.log").SingleOrDefault();
            AssertTrue(file is not null, "Expected a log file to be created.");

            var content = File.ReadAllText(file!);
            AssertTrue(content.Contains("Harness log entry", StringComparison.Ordinal), "Log entry content was not written.");
            return Task.CompletedTask;
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}' but got '{actual}'.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal sealed class FakeClock : ISystemClock
{
    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
