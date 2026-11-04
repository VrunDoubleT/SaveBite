using System.Text.Json;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Save order events atomically and publish persisted outbox messages to RabbitMQ.
public sealed class OrderEventPublisher : IOrderEventPublisher
{
    private readonly SaveBite.Backend.Data.AppDbContext _db;
    private readonly IOrderEventRepository _events;
    private readonly TimeProvider _clock;

    public OrderEventPublisher(IOrderEventRepository events, TimeProvider clock, SaveBite.Backend.Data.AppDbContext db)
    { _db = db; _events = events; _clock = clock; }

    // Persist completion alongside the state transition so a broker outage cannot lose earned progress.
    public async Task QueueCompletedAsync(Order order, CancellationToken ct = default)
    {
        var message = new OrderEventOutbox
        {
            Id = Guid.NewGuid(), OrderId = order.Id, EventKey = $"order:{order.Id}:completed", RoutingKey = RabbitMqRoutingKeys.OrderCompleted,
            PayloadJson = JsonSerializer.Serialize(new OrderCompletedEvent(order.Id, order.ShopId, order.CustomerId,
                order.CompletedAt ?? throw new InvalidOperationException("CompletedAt is required."))),
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        };
        await LifecycleOutbox.InventoryAsync(_db, order.Items.Select(i => i.FlashDealVariantId), order.Id, _clock.GetUtcNow().UtcDateTime, ct);
        _events.Add(message);
        await _events.SaveAsync(ct);
    }

    // NoShow has its own event so inventory, notifications and trust policies can be added independently.
    public async Task QueueNoShowAsync(Order order, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var message = new OrderEventOutbox
        {
            Id = Guid.NewGuid(), OrderId = order.Id, EventKey = $"order:{order.Id}:noshow", RoutingKey = RabbitMqRoutingKeys.OrderNoShow,
            PayloadJson = JsonSerializer.Serialize(new OrderNoShowEvent(order.Id, order.ShopId, order.CustomerId,
                order.HardDeadlineAt ?? throw new InvalidOperationException("The order has no pickup deadline."), now)),
            CreatedAt = now
        };
        await LifecycleOutbox.InventoryAsync(_db, order.Items.Select(i => i.FlashDealVariantId), order.Id, _clock.GetUtcNow().UtcDateTime, ct);
        _events.Add(message);
        await _events.SaveAsync(ct);
    }
}
