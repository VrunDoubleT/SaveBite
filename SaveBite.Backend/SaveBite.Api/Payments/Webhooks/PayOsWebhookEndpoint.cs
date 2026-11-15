using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed class PayOsWebhookEndpoint(IConfiguration configuration)
{
    public PayOsWebhookTarget Target(PayOsAccount account)
    {
        var dynamic = DeploymentEndpoint.ReadPublicEndpoint(configuration);
        var address = dynamic is null ? PayOsWebhookAddress.Resolve(configuration, account.Id == PayOsAccount.Platform)
            : dynamic.Url + (account.Id == PayOsAccount.Platform ? "/api/platform-fees/payos/webhook" : "/api/payments/payos/webhook");
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.IsLoopback)
            throw AppException.ServiceUnavailable("PayOS webhook synchronization requires a public HTTPS endpoint.");
        return new(account.Id, account.CredentialVersion, address, dynamic?.Session ?? "static");
    }
}
