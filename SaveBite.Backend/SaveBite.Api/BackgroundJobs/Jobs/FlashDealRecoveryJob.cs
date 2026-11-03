using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class FlashDealRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<FlashDealRecoveryJob> logger)
{
    [DisableConcurrentExecution(600)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await RecoveryBatch.RunAsync(scopes, logger, "flash-deal-close", db => db.FlashDeals
            .Where(d => d.OrderEndTime <= now && (d.Status == FlashDealStatus.Pending || d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut
                || d.Status == FlashDealStatus.Hidden || (d.Status == FlashDealStatus.Closed && d.LifecycleSyncedAt == null)))
            .Select(d => d.Id), (sp, id) => sp.GetRequiredService<IFlashDealJobService>().PublishDealExpiredAsync(id), ct);
        await RecoveryBatch.RunAsync(scopes, logger, "flash-deal-visibility", db => db.FlashDeals
            .Where(d => d.LifecycleSyncedAt == null && (d.Status == FlashDealStatus.Hidden || d.Status == FlashDealStatus.Cancelled))
            .Select(d => d.Id), async (sp, id) =>
            {
                var deal = await sp.GetRequiredService<AppDbContext>().FlashDeals.AsNoTracking().SingleAsync(d => d.Id == id, ct);
                await sp.GetRequiredService<SaveBite.Backend.Messaging.Interfaces.IRabbitMqPublisher>().PublishAsync(
                    deal.Status == FlashDealStatus.Hidden ? SaveBite.Backend.Messaging.Constants.RabbitMqRoutingKeys.FlashDealHidden
                        : SaveBite.Backend.Messaging.Constants.RabbitMqRoutingKeys.FlashDealClosed,
                    new SaveBite.Backend.Messaging.Events.FlashDealEvent { FlashDealId = id }, ct);
            }, ct);
        await RecoveryBatch.RunAsync(scopes, logger, "flash-deal-open", db => db.FlashDeals
            .Where(d => d.SaleStartTime <= now && d.OrderEndTime > now && (d.Status == FlashDealStatus.Pending
                || ((d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut) && d.LifecycleSyncedAt == null)))
            .Select(d => d.Id), (sp, id) => sp.GetRequiredService<IFlashDealJobService>().PublishDealOpenedAsync(id), ct);
    }
}
