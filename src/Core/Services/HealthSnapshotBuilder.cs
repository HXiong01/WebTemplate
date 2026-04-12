namespace WebTemplate.Core.Services;

public sealed class HealthSnapshotBuilder
{
    private readonly ISystemClock _clock;

    public HealthSnapshotBuilder(ISystemClock clock)
    {
        _clock = clock;
    }

    public HealthSnapshot Build(string service, string environment)
    {
        return new HealthSnapshot
        {
            Service = service,
            Status = "Healthy",
            CheckedAtUtc = _clock.UtcNow,
            Environment = environment
        };
    }
}
