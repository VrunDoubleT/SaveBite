using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;

namespace SaveBite.Backend.Services.Implementations;

public static class ShopDealOpenedOutbox
{
    // Caller owns the transaction changing Pending/OnSale and the lifecycle sync marker.
    public static async Task<Guid> StageAsync(AppDbContext db, Guid dealId, Guid shopId, DateTime openedAt, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var key = $"shop-deal-opened:{dealId}";
        var payload = JsonSerializer.Serialize(new ShopDealOpenedEvent(dealId, shopId, openedAt));
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO event_outbox (id, order_id, event_key, routing_key, payload_json, created_at)
            VALUES ({id}, NULL, {key}, {RabbitMqRoutingKeys.ShopDealOpened}, CAST({payload} AS jsonb), {openedAt})
            ON CONFLICT (event_key) DO NOTHING
            """, ct);
        // Return the original row on replay so immediate delivery and recovery share one message.
        return await db.Set<SaveBite.Backend.Models.Entities.OrderEventOutbox>().Where(e => e.EventKey == key)
            .Select(e => e.Id).SingleAsync(ct);
    }
}
