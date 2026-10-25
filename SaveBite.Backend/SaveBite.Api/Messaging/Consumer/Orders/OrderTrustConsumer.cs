using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Messaging.Consumer.Orders;

// Use a dedicated queue so trust effects never compete with other order event subscribers.
public sealed class OrderTrustConsumer : RabbitMqConsumer<OrderTrustConsumer.TerminalOrder>
{
    public sealed class TerminalOrder { public Guid OrderId { get; set; } }
    private readonly IServiceScopeFactory _scopes;
    public OrderTrustConsumer(IOptions<RabbitMqOptions> options, ILogger<OrderTrustConsumer> logger, IServiceScopeFactory scopes)
        : base(options, logger) { _scopes = scopes; }
    protected override string QueueName => RabbitMqQueues.OrderTrustQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.OrderTrustDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.OrderCompleted;
    protected override IReadOnlyCollection<string> RoutingKeys => [RabbitMqRoutingKeys.OrderCompleted, RabbitMqRoutingKeys.OrderNoShow];

    // Retry transient failures with fresh scoped contexts; successful duplicate deliveries are no-ops.
    protected override async Task HandleMessageAsync(RabbitMessage<TerminalOrder> message, CancellationToken cancellationToken)
    {
        if (message.Data == null || message.Data.OrderId == Guid.Empty) throw new ArgumentException("OrderId is required.");
        if (message.EventType is not (RabbitMqRoutingKeys.OrderCompleted or RabbitMqRoutingKeys.OrderNoShow))
            throw new ArgumentException("Unexpected terminal order event.");
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ITrustOrderService>().HandleAsync(message.Data.OrderId,
                    message.EventType == RabbitMqRoutingKeys.OrderCompleted, cancellationToken);
                return;
            }
            catch (Exception) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            { await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken); }
        }
    }
}
