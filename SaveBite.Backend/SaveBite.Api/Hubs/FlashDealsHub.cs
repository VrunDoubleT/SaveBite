using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SaveBite.Backend.Hubs;

// Public invalidation channel: no customer, order, stock or private shop payloads.
[AllowAnonymous]
public sealed class FlashDealsHub(ILogger<FlashDealsHub> logger) : Hub<IApplicationClient>
{
    public const string Path = "/api/flash-deals-realtime";
    public const string FeedGroupName = "public-feed";
    public static string ShopGroup(Guid id) => $"public-shop:{id:D}";
    public static string DealGroup(Guid id) => $"public-deal:{id:D}";
    public static string FeedGroup() => FeedGroupName;

    public async Task WatchFeed()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, FeedGroupName);
        logger.LogInformation("Flash deal realtime subscribed: connection {ConnectionId}, feed group.", Context.ConnectionId);
    }

    public async Task WatchAll()
    {
        await WatchFeed();
    }

    public async Task WatchShop(Guid shopId)
    {
        if (shopId == Guid.Empty) throw new HubException("Shop ID required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ShopGroup(shopId));
        logger.LogInformation("Flash deal realtime subscribed: connection {ConnectionId}, shop {ShopId}.", Context.ConnectionId, shopId);
    }

    public async Task WatchDeal(Guid dealId)
    {
        if (dealId == Guid.Empty) throw new HubException("Deal ID required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, DealGroup(dealId));
        logger.LogInformation("Flash deal realtime subscribed: connection {ConnectionId}, deal {DealId}.", Context.ConnectionId, dealId);
    }
}
