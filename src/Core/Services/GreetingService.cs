namespace WebTemplate.Core.Services;

public sealed class GreetingService
{
    public string CreateGreeting(string? name)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "Developer" : name.Trim();
        return $"Hello, {normalizedName}. WebTemplate is running.";
    }
}
