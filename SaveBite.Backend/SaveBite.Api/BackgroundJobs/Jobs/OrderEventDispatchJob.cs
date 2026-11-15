using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Dispatch a persisted outbox event through the order event publisher.
public sealed class OrderEventDispatchJob
{
    private readonly SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState _topology;
    private readonly IOrderEventRepository _events;
    private readonly IRabbitMqPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<SaveBite.Backend.Hubs.FlashDealsHub, SaveBite.Backend.Hubs.IApplicationClient>? _dealHub;
    private readonly IRealtimePublisher? _realtime;
    private readonly ILocationService? _locationService;
    private readonly AppDbContext? _db;

    public OrderEventDispatchJob(
        IOrderEventRepository events,
        IRabbitMqPublisher publisher,
        TimeProvider clock,
        SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState topology,
        IRealtimePublisher? realtime = null,
        Microsoft.AspNetCore.SignalR.IHubContext<SaveBite.Backend.Hubs.FlashDealsHub, SaveBite.Backend.Hubs.IApplicationClient>? dealHub = null,
        ILocationService? locationService = null, AppDbContext? db = null)
    {
        _dealHub = dealHub;
        _realtime = realtime;
        _topology = topology;
        _events = events;
        _publisher = publisher;
        _clock = clock;
        _locationService = locationService;
        _db = db;
    }

    // Broker failures bubble up to Hangfire retry; a duplicate delivery remains safe for the consumer.
    public async Task ExecuteAsync(Guid messageId, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        ct = timeout.Token;
        await _events.DispatchAsync(messageId, async message =>
        {
            if (!message.RoutingKey.StartsWith("internal.", StringComparison.Ordinal))
                await _topology.Ready.WaitAsync(TimeSpan.FromSeconds(10), ct);
            switch (message.RoutingKey)
            {
                case SaveBite.Backend.Services.Implementations.FlashDealRealtimeOutbox.Route:
                    var change = JsonSerializer.Deserialize<SaveBite.Backend.Services.Implementations.FlashDealChanged>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid deal change.");
                    var envelope = new SaveBite.Backend.Models.DTOs.RealtimeEvent(Guid.NewGuid(),
                        SaveBite.Backend.Services.Implementations.FlashDealRealtimeOutbox.Event, 1, _clock.GetUtcNow().UtcDateTime, change);
                    var hub = _dealHub ?? throw new InvalidOperationException("Public deal hub required.");
                    await hub.Clients.Groups(new[] {
                        SaveBite.Backend.Hubs.FlashDealsHub.DealGroup(change.DealId),
                        SaveBite.Backend.Hubs.FlashDealsHub.ShopGroup(change.ShopId),
                        SaveBite.Backend.Hubs.FlashDealsHub.FeedGroup()
                    }).ReceiveEvent(envelope).WaitAsync(ct);
                    await hub.Clients.All.ReceiveEvent(envelope).WaitAsync(ct);
                    break;
                case RabbitMqRoutingKeys.ShopDealOpened:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<ShopDealOpenedEvent>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid shop deal opening event."), ct);
                    break;
                case RabbitMqRoutingKeys.UserDefaultAddressChanged:
                    var location = _locationService ?? throw new InvalidOperationException("Location service is required.");
                    var db = _db ?? throw new InvalidOperationException("The business DbContext is required.");
                    var addressEvt = JsonSerializer.Deserialize<UserDefaultAddressChangedEvent>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid default address event.");
                    // Outbox IDs do not encode event order. Serialize deliveries per user and
                    // project the latest committed default instead of replaying stale coordinates/deletes.
                    var locationKey = $"default-address-sync:{addressEvt.UserId}";
                    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({locationKey}, 0))", ct);
                    var current = await db.UserAddresses.AsNoTracking()
                        .SingleOrDefaultAsync(a => a.UserId == addressEvt.UserId && a.IsDefault, ct);
                    if (current == null) await location.RemoveDefaultAddressAsync(addressEvt.UserId, ct);
                    else await location.SyncDefaultAddressAsync(current.UserId, current.Latitude, current.Longitude, current.AddressVersion, ct);
                    break;
                case SaveBite.Backend.Services.Implementations.FeedbackRealtimeOutbox.Route:
                    var feedback = JsonSerializer.Deserialize<JsonElement>(message.PayloadJson);
                    await (_realtime ?? throw new InvalidOperationException("Realtime publisher is required."))
                        .SendToAllAsync(SaveBite.Backend.Constants.RealtimeEvents.FeedbackChanged, feedback).WaitAsync(ct);
                    await (_dealHub ?? throw new InvalidOperationException("Public deal hub is required."))
                        .Clients.All.ReceiveEvent(new SaveBite.Backend.Models.DTOs.RealtimeEvent(message.Id,
                            SaveBite.Backend.Constants.RealtimeEvents.FeedbackChanged, 1, _clock.GetUtcNow().UtcDateTime, feedback)).WaitAsync(ct);
                    break;
                case "internal.cart.refresh":
                    var refresh = JsonSerializer.Deserialize<CartRefreshEvent>(message.PayloadJson)
                        ?? throw new InvalidOperationException("Invalid cart refresh request.");
                    await (_realtime ?? throw new InvalidOperationException("Realtime publisher is required."))
                        .SendToUserAsync(refresh.UserId, SaveBite.Backend.Constants.RealtimeEvents.CartChanged,
                            new SaveBite.Backend.Models.DTOs.CartChangedEvent(SaveBite.Backend.Constants.RealtimeEvents.CartReasons.DealUnavailable)).WaitAsync(ct);
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
                case RabbitMqRoutingKeys.FlashDealOpened:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<FlashDealEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid opened-deal event."), ct);
                    break;
                case RabbitMqRoutingKeys.FlashDealHidden:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<FlashDealEvent>(message.PayloadJson) ?? throw new InvalidOperationException("Invalid hidden-deal event."), ct);
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
                case RabbitMqRoutingKeys.PlatformPayOsVerified:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<PlatformPayOsVerifiedEvent>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid payOS activation event."), ct);
                    break;
                case RabbitMqRoutingKeys.TrustLevelReconcile:
                    await _publisher.PublishAsync(message.RoutingKey,
                        JsonSerializer.Deserialize<TrustLevelReconcileEvent>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid trust level reconcile event."), ct);
                    break;
                default: throw new InvalidOperationException("Unsupported order event.");
            }
            message.PublishedAt = _clock.GetUtcNow().UtcDateTime;
        }, ct);
    }
}
