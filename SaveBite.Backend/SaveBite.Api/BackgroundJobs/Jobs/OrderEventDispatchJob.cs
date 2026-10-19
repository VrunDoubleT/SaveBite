using System.Text.Json;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Dispatch a persisted outbox event through the order event publisher.
public sealed class OrderEventDispatchJob
{
    private readonly IOrderEventRepository _events;
    private readonly IRabbitMqPublisher _publisher;
    private readonly TimeProvider _clock;

    public OrderEventDispatchJob(IOrderEventRepository events, IRabbitMqPublisher publisher, TimeProvider clock)
    { _events = events; _publisher = publisher; _clock = clock; }

    // Broker failures bubble up to Hangfire retry; a duplicate delivery remains safe for the consumer.
    public Task ExecuteAsync(Guid messageId, CancellationToken ct) => _events.DispatchAsync(messageId, async message =>
    {
        switch (message.RoutingKey)
        {
            case RabbitMqRoutingKeys.OrderPending:
                await _publisher.PublishAsync(message.RoutingKey,
                    JsonSerializer.Deserialize<OrderPendingEvent>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid pending order event."), ct);
                break;
            case RabbitMqRoutingKeys.OrderNoShow:
                await _publisher.PublishAsync(message.RoutingKey,
                    JsonSerializer.Deserialize<OrderNoShowEvent>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid NoShow order event."), ct);
                break;
            default: throw new InvalidOperationException("Unsupported order event.");
        }
        message.PublishedAt = _clock.GetUtcNow().UtcDateTime;
    }, ct);
}
