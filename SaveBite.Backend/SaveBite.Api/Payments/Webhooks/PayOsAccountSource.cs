using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Payments.Webhooks;

public interface IPayOsAccountSource
{
    Task<PayOsAccount?> ReadAsync(string accountId, CancellationToken ct);
}

public sealed class PayOsAccountSource(AppDbContext db, IConfiguration configuration, IDataProtectionProvider protection) : IPayOsAccountSource
{
    public async Task<PayOsAccount?> ReadAsync(string accountId, CancellationToken ct)
    {
        if (accountId == PayOsAccount.Platform)
        {
            var stored = await db.PlatformPayOsConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
            if (stored is not null)
            {
                var protector = protection.CreateProtector("SaveBite.PlatformPayOsConfig.v1");
                return new(accountId, new(stored.ClientId, protector.Unprotect(stored.ProtectedApiKey),
                    protector.Unprotect(stored.ProtectedChecksumKey)), stored.Revision,
                    stored.IsInvalid || stored.VerifiedAt == null);
            }
            var credentials = new PlatformPayOsCredentials(configuration["PlatformPayOS:ClientId"] ?? "",
                configuration["PlatformPayOS:ApiKey"] ?? "", configuration["PlatformPayOS:ChecksumKey"] ?? "");
            return Complete(credentials) ? new(accountId, credentials) : null;
        }
        if (!accountId.StartsWith("shop:", StringComparison.Ordinal) || !Guid.TryParse(accountId[5..], out var shopId)) return null;
        var shop = await db.ShopPaymentConfigs.AsNoTracking().SingleOrDefaultAsync(x => x.ShopId == shopId, ct);
        if (shop is null) return null;
        var keys = new PlatformPayOsCredentials(shop.PayosClientId, shop.PayosApiKey, shop.PayosChecksumKey);
        return Complete(keys) ? new(accountId, keys) : null;
    }

    private static bool Complete(PlatformPayOsCredentials credentials)
        => new[] { credentials.ClientId, credentials.ApiKey, credentials.ChecksumKey }
            .All(value => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("seed-", StringComparison.OrdinalIgnoreCase));
}
