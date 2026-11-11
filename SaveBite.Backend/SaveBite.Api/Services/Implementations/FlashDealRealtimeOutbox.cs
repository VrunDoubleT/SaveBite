using System.Text.Json;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Implementations;

public sealed record FlashDealChanged(Guid DealId, Guid ShopId);
public static class FlashDealRealtimeOutbox
{
    public const string Route = "internal.flashdeal.changed";
    public const string Event = "flashdeal.changed";
    public static void Stage(AppDbContext db, Guid dealId, Guid shopId)
    {
        if (db.Set<OrderEventOutbox>().Local.Any(e => e.RoutingKey == Route && e.PublishedAt == null
            && e.PayloadJson == JsonSerializer.Serialize(new FlashDealChanged(dealId, shopId)))) return;
        db.Add(new OrderEventOutbox { Id = Guid.NewGuid(), EventKey = $"deal-change:{dealId}:{Guid.NewGuid()}",
            RoutingKey = Route, PayloadJson = JsonSerializer.Serialize(new FlashDealChanged(dealId, shopId)), CreatedAt = DateTime.UtcNow });
    }
}
