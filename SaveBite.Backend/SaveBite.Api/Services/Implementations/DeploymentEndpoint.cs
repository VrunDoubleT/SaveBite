using System.Text.Json;
using SaveBite.Backend.Exceptions;

namespace SaveBite.Backend.Services.Implementations;

public static class DeploymentEndpoint
{
    // Local development retains its existing settings; containers use the watcher's snapshot.
    public static string Resolve(IConfiguration configuration, string setting, string? fallback = null)
    {
        return ReadPublicEndpoint(configuration)?.Url ?? configuration[setting] ?? fallback ?? string.Empty;
    }

    public static PublicEndpoint? ReadPublicEndpoint(IConfiguration configuration)
    {
        var path = configuration["Deployment:PublicEndpointFile"];
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            var state = JsonSerializer.Deserialize<EndpointSnapshot>(File.ReadAllText(path));
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (state?.UpdatedAtUnix ?? 0);
            if (state is null || !state.Ready || age is < -10 or >= 45
                || !Uri.TryCreate(state.Url, UriKind.Absolute, out var uri)
                || uri.Scheme != "https" || !uri.Host.EndsWith(".trycloudflare.com", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/" || !uri.IsDefaultPort)
                throw AppException.ServiceUnavailable("The public tunnel is not ready. Please retry shortly.");
            var session = state.Session ?? uri.GetLeftPart(UriPartial.Authority);
            if (session.Length is 0 or > 256)
                throw AppException.ServiceUnavailable("The public tunnel is not ready. Please retry shortly.");
            return new(uri.GetLeftPart(UriPartial.Authority), session);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw AppException.ServiceUnavailable("The public tunnel is not ready. Please retry shortly.");
        }
    }

    public static void EnsurePaymentCreationEnabled(IConfiguration configuration)
    {
        if (!configuration.GetValue("Deployment:PaymentCreationEnabled", true))
            throw AppException.ServiceUnavailable("Online payment creation is temporarily disabled.");
    }

    public sealed record PublicEndpoint(string Url, string Session);
    private sealed record EndpointSnapshot(string Url, bool Ready, long UpdatedAtUnix, string? Session = null);
}
