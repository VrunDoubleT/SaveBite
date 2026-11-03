using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Services.Implementations;

// Call inside the caller's transaction; never publish to a broker before commit.
public static class LifecycleOutbox
{
    public static void RefundTransferred(AppDbContext db, Refund refund, DateTime now)
    {
        var key = $"refund:{refund.Id}:{refund.TransferredAmount.ToString("G29", CultureInfo.InvariantCulture)}";
        if (db.Set<OrderEventOutbox>().Local.Any(m => m.EventKey == key)) return;
        db.Add(new OrderEventOutbox { Id = Guid.NewGuid(), OrderId = refund.OrderId, EventKey = key,
            RoutingKey = RabbitMqRoutingKeys.RefundTransferred,
            PayloadJson = JsonSerializer.Serialize(new RefundTransferredEvent(refund.Id, refund.OrderId, refund.TransferredAmount)), CreatedAt = now });
    }
    public static async Task InventoryAsync(AppDbContext db, IEnumerable<Guid> variantIds, Guid? orderId, DateTime now, CancellationToken ct)
    {
        var ids = variantIds.Distinct().ToArray();
        if (ids.Length == 0) return;
        var deals = await db.FlashDealVariants.Where(v => ids.Contains(v.Id)).Select(v => v.FlashDealId).Distinct().ToArrayAsync(ct);
        InventoryDeals(db, deals, orderId, now);
    }
    public static void InventoryDeals(AppDbContext db, Guid[] deals, Guid? orderId, DateTime now)
    {
        if (deals.Length == 0) return;
        db.Add(new OrderEventOutbox { Id = Guid.NewGuid(), OrderId = orderId, EventKey = $"inventory:{Guid.NewGuid():N}",
            RoutingKey = RabbitMqRoutingKeys.InventoryChanged, PayloadJson = JsonSerializer.Serialize(new InventoryChangedEvent(deals)), CreatedAt = now });
    }
}
