namespace WebTemplate.Core.Services;

public sealed class HealthSnapshot
{
    public required string Service { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CheckedAtUtc { get; init; }
    public required string Environment { get; init; }
}
