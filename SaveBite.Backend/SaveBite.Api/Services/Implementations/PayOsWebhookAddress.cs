using SaveBite.Backend.Exceptions;

namespace SaveBite.Backend.Services.Implementations;

public static class PayOsWebhookAddress
{
    public static string Resolve(IConfiguration configuration, bool platformFees = false)
    {
        var backend = configuration["Checkout:BackendBaseUrl"]?.Trim();
        if (string.IsNullOrEmpty(backend))
        {
            if (!int.TryParse(configuration["API_PORT"], out var port) || port is < 1 or > 65535)
                throw AppException.ServiceUnavailable("Set API_PORT or Checkout:BackendBaseUrl for PayOS webhooks.");
            backend = $"http://localhost:{port}";
        }
        if (!Uri.TryCreate(backend, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw AppException.ServiceUnavailable("Checkout:BackendBaseUrl must be an HTTP(S) backend base URL without credentials, query or fragment.");
        return backend.TrimEnd('/') + (platformFees ? "/api/platform-fees/payos/webhook" : "/api/payments/payos/webhook");
    }
}
