using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Orders;

/// <summary>
/// Accrues platform fees asynchronously when an order reaches Completed status (BR-093).
/// </summary>
public sealed class OrderPlatformFeeConsumer : RabbitMqConsumer<OrderPlatformFeeConsumer.OrderCompletedData>
{
    public sealed class OrderCompletedData
    {
        public Guid OrderId { get; set; }
    }

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OrderPlatformFeeConsumer> _logger;

    public OrderPlatformFeeConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<OrderPlatformFeeConsumer> logger,
        IServiceScopeFactory scopes)
        : base(options, logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override string QueueName => RabbitMqQueues.OrderPlatformFeeQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.OrderPlatformFeeDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.OrderCompleted;

    protected override async Task HandleMessageAsync(RabbitMessage<OrderCompletedData> message, CancellationToken cancellationToken)
    {
        if (message.Data == null || message.Data.OrderId == Guid.Empty)
        {
            throw new ArgumentException("OrderId is required for platform fee accrual.");
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var statementService = scope.ServiceProvider.GetRequiredService<IPlatformFeeStatementService>();
                await statementService.AccrueOrderPlatformFeeAsync(message.Data.OrderId, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Transient failure processing platform fee accrual for Order {OrderId}. Retrying attempt {Attempt}...", message.Data.OrderId, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
            }
        }
    }
}
