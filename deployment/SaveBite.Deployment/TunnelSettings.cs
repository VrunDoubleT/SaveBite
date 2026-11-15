namespace SaveBite.Deployment;

public sealed record TunnelSettings
{
    public const string DefaultOrigin = "http://127.0.0.1:80";
    public const string DefaultStatePath = "/var/lib/savebite/public-endpoint.json";
    public string Executable { get; init; } = "cloudflared";
    public string Origin { get; init; } = DefaultOrigin;
    public string StatePath { get; init; } = DefaultStatePath;
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan UnhealthyTimeout { get; init; } = TimeSpan.FromSeconds(180);
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public static TunnelSettings FromEnvironment() => new()
    {
        Executable = Environment.GetEnvironmentVariable("CLOUDFLARED_PATH") ?? "cloudflared",
        Origin = Environment.GetEnvironmentVariable("TUNNEL_ORIGIN") ?? DefaultOrigin,
        StatePath = Environment.GetEnvironmentVariable("PUBLIC_ENDPOINT_FILE") ?? DefaultStatePath
    };

    public static TimeSpan RetryDelay(int attempt, double jitter)
        => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, Math.Clamp(attempt, 0, 6))) + Math.Clamp(jitter, 0, 1));
}
