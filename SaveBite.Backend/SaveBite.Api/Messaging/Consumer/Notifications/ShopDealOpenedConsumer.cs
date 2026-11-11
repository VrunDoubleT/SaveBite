using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;

namespace SaveBite.Backend.Messaging.Consumer.Notifications;

// Dedicated subscriber owns follower fan-out; unrelated customers never receive this broadcast.
public sealed class ShopDealOpenedConsumer(IOptions<RabbitMqOptions> options, ILogger<ShopDealOpenedConsumer> logger,
    IServiceScopeFactory scopes) : RabbitMqConsumer<ShopDealOpenedEvent>(options, logger)
{
    protected override string QueueName => RabbitMqQueues.ShopDealOpenedQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.ShopDealOpened;
    protected override string? DeadLetterQueueName => QueueName + "_dead_letter";
    protected override int RetryAttempts => 3;

    protected override async Task HandleMessageAsync(RabbitMessage<ShopDealOpenedEvent> message, CancellationToken ct)
    {
        var data = message.Data;
        if (message.EventType != RoutingKey || data == null || data.FlashDealId == Guid.Empty || data.ShopId == Guid.Empty
            || data.OpenedAt == default || data.OpenedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A valid shop deal opening event is required.");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation("Deal notification received {DealId}, message {MessageId}: sincePublish={SincePublishMs}ms, sinceOpening={SinceOpeningMs}ms.",
            data.FlashDealId, message.MessageId, (DateTime.UtcNow - message.CreatedAt).TotalMilliseconds,
            (DateTime.UtcNow - data.OpenedAt).TotalMilliseconds);
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SaveBite.Backend.Services.Implementations.ShopDealNotificationService>()
            .HandleAsync(data, ct);
        logger.LogInformation("Deal notification handled {DealId}: {ElapsedMs}ms.", data.FlashDealId, timer.ElapsedMilliseconds);
    }
}
