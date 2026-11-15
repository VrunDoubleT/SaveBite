using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed class PayOsWebhookReadiness(AppDbContext db, IPayOsAccountSource accounts, PayOsWebhookEndpoint endpoint,
    IConfiguration configuration)
{
    public async Task<bool> IsPlatformReadyAsync(CancellationToken ct)
    {
        try { return await IsReadyAsync(PayOsAccount.Platform, null, ct); }
        catch (AppException) { return false; }
    }

    public async Task EnsureShopReadyAsync(ShopPaymentConfig config, CancellationToken ct)
    {
        DeploymentEndpoint.EnsurePaymentCreationEnabled(configuration);
        var expected = new PayOsAccount(PayOsAccount.Shop(config.ShopId), new(config.PayosClientId, config.PayosApiKey, config.PayosChecksumKey));
        if (!await IsReadyAsync(expected.Id, expected.CredentialVersion, ct))
            throw AppException.ServiceUnavailable("This shop's PayOS webhook is synchronizing. Please retry shortly.");
    }

    private async Task<bool> IsReadyAsync(string accountId, string? expectedVersion, CancellationToken ct)
    {
        var account = await accounts.ReadAsync(accountId, ct);
        if (account is null || account.NeedsValidation || (expectedVersion is not null && account.CredentialVersion != expectedVersion)) return false;
        var target = endpoint.Target(account);
        var state = await db.Set<PayOsWebhookSyncState>().AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (!target.IsReady(state)) return false;
        // Re-read the file after the database round-trip: a changed session invalidates readiness immediately.
        return target == endpoint.Target(account);
    }
}
