using SaveBite.Backend.Configurations;
using Hangfire;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Cart completion is independent of the Redis lifecycle marker.
public sealed class CartAvailabilityRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<CartAvailabilityRecoveryJob> logger)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return RecoveryBatch.RunAsync(scopes, logger, "cart-availability", db => db.CartItems
            .Where(i => i.FlashDealVariant.FlashDeal.Status == FlashDealStatus.Hidden
                || i.FlashDealVariant.FlashDeal.Status == FlashDealStatus.Cancelled
                || i.FlashDealVariant.FlashDeal.Status == FlashDealStatus.Closed
                || i.FlashDealVariant.FlashDeal.OrderEndTime <= now)
            .Select(i => i.FlashDealVariant.FlashDealId).Distinct(),
            (sp, id) => sp.GetRequiredService<ICartDealEventService>().HandleAsync(id, ct), ct);
    }
}
