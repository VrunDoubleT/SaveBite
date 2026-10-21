using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Orders;

// Consume paid-order events and schedule the shop confirmation timeout.
public sealed class OrderPendingConsumer : RabbitMqConsumer<OrderPendingEvent>
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OrderPendingConsumer> _logger;
    public OrderPendingConsumer(IOptions<RabbitMqOptions> options, ILogger<OrderPendingConsumer> logger, IServiceScopeFactory scopes)
        : base(options, logger) { _scopes = scopes; _logger = logger; }

    protected override string QueueName => RabbitMqQueues.OrderPendingQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.OrderPending;
    protected override string? DeadLetterQueueName => RabbitMqQueues.OrderPendingDeadLetterQueueName;

    // Validate the pending-order event and schedule its confirmation deadline through a scoped service.
    protected override async Task HandleMessageAsync(RabbitMessage<OrderPendingEvent> message, CancellationToken cancellationToken)
    {
        if (message.Data == null || message.Data.OrderId == Guid.Empty || message.Data.ShopId == Guid.Empty)
            throw new ArgumentException("OrderPending requires OrderId and ShopId.");
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IShopOrderEventService>()
                    .HandlePendingAsync(message.Data.OrderId, message.Data.ShopId, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Retrying OrderPending {OrderId}, attempt {Attempt}.", message.Data.OrderId, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
            }
        }
    }
}
