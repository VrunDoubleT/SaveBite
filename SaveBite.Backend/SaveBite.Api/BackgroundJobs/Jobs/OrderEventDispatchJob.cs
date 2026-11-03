using System.Text.Json;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Dispatch a persisted outbox event through the order event publisher.
public sealed class OrderEventDispatchJob
{
    private readonly SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState _topology;
    private readonly IOrderEventRepository _events;
    private readonly IRabbitMqPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly SaveBite.Backend.Services.Interfaces.IRealtimePublisher? _realtime;

    public OrderEventDispatchJob(IOrderEventRepository events, IRabbitMqPublisher publisher, TimeProvider clock, SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState topology, SaveBite.Backend.Services.Interfaces.IRealtimePublisher? realtime = null)
    { _realtime = realtime; _topology = topology; _events = events; _publisher = publisher; _clock = clock; }

    // Broker failures bubble up to Hangfire retry; a duplicate delivery remains safe for the consumer.
    public async Task ExecuteAsync(Guid messageId, CancellationToken ct)
    {
        await _topology.Ready.WaitAsync(ct);
        await _events.DispatchAsync(messageId, async message =>
        {
            switch (message.RoutingKey)
            {
                case "internal.cart.refresh":
                    var refresh = JsonSerializer.Deserialize<CartRefreshEvent>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid cart refresh request.");
                    await (_realtime ?? throw new InvalidOperationException("Realtime publisher is required."))
                        .SendToUserAsync(refresh.UserId, SaveBite.Backend.Constants.RealtimeEvents.CartChanged,
                            new SaveBite.Backend.Models.DTOs.CartChangedEvent(SaveBite.Backend.Constants.RealtimeEvents.CartReasons.DealUnavailable));
                    break;
                case RabbitMqRoutingKeys.EmailRoutingKey:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<EmailNotificationEvent>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid email request."), ct);
                    break;
                case RabbitMqRoutingKeys.OrderCompleted:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<OrderCompletedEvent>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid completed order event."), ct);
                    break;
                case RabbitMqRoutingKeys.NotificationRequested:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<NotificationRequestedEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid notification event."), ct);
                    break;
                case RabbitMqRoutingKeys.UserVerified:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<UserVerifiedEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid verified-account event."), ct);
                    break;
                case RabbitMqRoutingKeys.FlashDealClosed:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<FlashDealEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid closed-deal event."), ct);
                    break;
                case RabbitMqRoutingKeys.OrderPending:
                    // Legacy outbox rows: Pending deadlines are now registered directly and recovered by the Order sweep.
                    break;
                case RabbitMqRoutingKeys.RefundTransferred:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<RefundTransferredEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid refund event."), ct);
                    break;
                case RabbitMqRoutingKeys.InventoryChanged:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<InventoryChangedEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid inventory event."), ct);
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
}
