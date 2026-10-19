using System.Text.Json;
using SaveBite.Backend.BackgroundJobs.Interfaces;
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
    private readonly IOrderEventRepository _events;
    private readonly IOrderEventDispatchScheduler _scheduler;
    private readonly TimeProvider _clock;

    public OrderEventPublisher(IOrderEventRepository events, IOrderEventDispatchScheduler scheduler, TimeProvider clock)
    { _events = events; _scheduler = scheduler; _clock = clock; }

    // Called inside the order transaction so Pending and its durable event either commit together or both roll back.
    public async Task QueuePendingAsync(Order order, CancellationToken ct = default)
    {
        var pendingAt = CheckoutOrderMapper.RequiredPayment(order)?.PaidAt ?? order.CreatedAt;
        var message = new OrderEventOutbox
        {
            Id = Guid.NewGuid(), OrderId = order.Id, RoutingKey = RabbitMqRoutingKeys.OrderPending,
            PayloadJson = JsonSerializer.Serialize(new OrderPendingEvent(order.Id, order.ShopId, pendingAt, pendingAt.AddMinutes(5))),
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        };
        _events.Add(message);
        await _events.SaveAsync(ct);
        _scheduler.Enqueue(message.Id);
    }
    // NoShow has its own event so inventory, notifications and trust policies can be added independently.
    public async Task QueueNoShowAsync(Order order, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var message = new OrderEventOutbox
        {
            Id = Guid.NewGuid(), OrderId = order.Id, RoutingKey = RabbitMqRoutingKeys.OrderNoShow,
            PayloadJson = JsonSerializer.Serialize(new OrderNoShowEvent(order.Id, order.ShopId, order.CustomerId,
                order.HardDeadlineAt ?? throw new InvalidOperationException("The order has no pickup deadline."), now)),
            CreatedAt = now
        };
        _events.Add(message);
        await _events.SaveAsync(ct);
        _scheduler.Enqueue(message.Id);
    }
}
