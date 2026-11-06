using SaveBite.Backend.Constants;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopStatusEventService(
    IShopOrderRepository shops,
    IRealtimePublisher realtime,
    ILogger<ShopStatusEventService> logger) : IShopStatusEventService
{
    public async Task NotifyChangedAsync(Guid shopId, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipients = await shops.RealtimeRecipientsAsync(shopId, cancellationToken);
            foreach (var userId in recipients)
                await realtime.SendToUserAsync(userId, RealtimeEvents.ShopStatusChanged, new { shopId, status });
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not publish status change for shop {ShopId}.", shopId);
        }
    }
}
