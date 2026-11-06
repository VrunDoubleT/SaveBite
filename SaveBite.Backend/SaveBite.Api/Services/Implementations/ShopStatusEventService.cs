using Microsoft.AspNetCore.SignalR;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Hubs;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopStatusEventService(
    IShopOrderRepository shops,
    IRealtimePublisher realtime,
    IHubContext<FlashDealsHub, IApplicationClient> dealHub,
    ILogger<ShopStatusEventService> logger) : IShopStatusEventService
{
    public async Task NotifyChangedAsync(Guid shopId, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var isBlocked = !string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
            var eventData = new { shopId, status, isBlocked };

            var recipients = await shops.RealtimeRecipientsAsync(shopId, cancellationToken);
            foreach (var userId in recipients)
                await realtime.SendToUserAsync(userId, RealtimeEvents.ShopStatusChanged, eventData);

            // Broadcast to all authenticated clients (e.g. customers browsing or on home page)
            await realtime.SendToAllAsync(RealtimeEvents.ShopStatusChanged, eventData);

            // Broadcast to all public clients watching flash deals / home page
            var envelope = new RealtimeEvent(Guid.NewGuid(), RealtimeEvents.ShopStatusChanged, 1, DateTime.UtcNow, eventData);
            await dealHub.Clients.All.ReceiveEvent(envelope);
            await dealHub.Clients.Group(FlashDealsHub.ShopGroup(shopId)).ReceiveEvent(envelope);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not publish status change for shop {ShopId}.", shopId);
        }
    }
}
