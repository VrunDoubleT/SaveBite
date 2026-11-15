using System.Text.RegularExpressions;

namespace SaveBite.Deployment;

// Keep this JSON contract compatible with the API's DeploymentEndpoint reader.
public sealed partial record EndpointSnapshot(string Url, bool Ready, string Session, long UpdatedAtUnix)
{
    [GeneratedRegex(@"https://[a-z0-9]+(?:-[a-z0-9]+)*\.trycloudflare\.com(?![a-zA-Z0-9_.:\-])", RegexOptions.CultureInvariant)]
    private static partial Regex TunnelUrlPattern();

    public static string? ExtractUrl(string line)
    {
        var match = TunnelUrlPattern().Match(line);
        return match.Success ? match.Value : null;
    }

    public bool IsHealthy(TimeProvider clock)
    {
        var age = clock.GetUtcNow().ToUnixTimeSeconds() - UpdatedAtUnix;
        if (!Ready || string.IsNullOrWhiteSpace(Session) || age is < -10 or >= 45 || string.IsNullOrWhiteSpace(Url))
            return false;
        var match = TunnelUrlPattern().Match(Url);
        return match.Success && match.Index == 0 && match.Length == Url.Length;
    }
}
