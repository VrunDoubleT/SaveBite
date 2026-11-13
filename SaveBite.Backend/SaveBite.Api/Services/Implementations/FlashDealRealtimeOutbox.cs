using System.Text.Json;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Implementations;

public sealed record FlashDealChanged(
    Guid DealId,
    Guid ShopId,
    string? Action = null,
    string? Status = null,
    long? VisibilityVersion = null);

public static class FlashDealRealtimeOutbox
{
    public const string Route = "internal.flashdeal.changed";
    public const string Event = "flashdeal.changed";

    public static void Stage(
        AppDbContext db,
        Guid dealId,
        Guid shopId,
        string? action = null,
        string? status = null,
        long? visibilityVersion = null)
    {
        var model = new FlashDealChanged(dealId, shopId, action, status, visibilityVersion);
        var payload = JsonSerializer.Serialize(model);
        if (db.Set<OrderEventOutbox>().Local.Any(e => e.RoutingKey == Route && e.PublishedAt == null
            && e.PayloadJson == payload)) return;

        db.Add(new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            EventKey = $"deal-change:{dealId}:{Guid.NewGuid()}",
            RoutingKey = Route,
            PayloadJson = payload,
            CreatedAt = DateTime.UtcNow
        });
    }
}
