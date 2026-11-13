using System.Text.Json;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Implementations;

public static class FlashDealLifecycleOutbox
{
    public static void StageOpened(AppDbContext db, Guid dealId, DateTime? expectedDeadline = null, DateTime? now = null)
    {
        var createdAt = now ?? DateTime.UtcNow;
        var key = $"flashdeal-opened:{dealId}:{Guid.NewGuid():N}";
        var payload = JsonSerializer.Serialize(new FlashDealEvent
        {
            FlashDealId = dealId,
            ExpectedDeadline = expectedDeadline
        });

        var outbox = new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            OrderId = null,
            EventKey = key,
            RoutingKey = RabbitMqRoutingKeys.FlashDealOpened,
            PayloadJson = payload,
            CreatedAt = createdAt
        };

        db.Set<OrderEventOutbox>().Add(outbox);
    }

    public static void StageClosed(AppDbContext db, Guid dealId, DateTime? now = null)
    {
        var createdAt = now ?? DateTime.UtcNow;
        var key = $"flashdeal-closed:{dealId}:{Guid.NewGuid():N}";
        var payload = JsonSerializer.Serialize(new FlashDealEvent
        {
            FlashDealId = dealId
        });

        var outbox = new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            OrderId = null,
            EventKey = key,
            RoutingKey = RabbitMqRoutingKeys.FlashDealClosed,
            PayloadJson = payload,
            CreatedAt = createdAt
        };

        db.Set<OrderEventOutbox>().Add(outbox);
    }

    public static void StageHidden(AppDbContext db, Guid dealId, DateTime? now = null)
    {
        var createdAt = now ?? DateTime.UtcNow;
        var key = $"flashdeal-hidden:{dealId}:{Guid.NewGuid():N}";
        var payload = JsonSerializer.Serialize(new FlashDealEvent
        {
            FlashDealId = dealId
        });

        var outbox = new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            OrderId = null,
            EventKey = key,
            RoutingKey = RabbitMqRoutingKeys.FlashDealHidden,
            PayloadJson = payload,
            CreatedAt = createdAt
        };

        db.Set<OrderEventOutbox>().Add(outbox);
    }
}
