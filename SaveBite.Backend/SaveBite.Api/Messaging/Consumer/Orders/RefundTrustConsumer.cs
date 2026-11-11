using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.Messaging.Consumer.Orders;

public sealed class RefundTrustConsumer(IOptions<RabbitMqOptions> options, ILogger<RefundTrustConsumer> logger, IServiceScopeFactory scopes)
    : RabbitMqConsumer<RefundTransferredEvent>(options, logger)
{
    protected override string QueueName => RabbitMqQueues.RefundTrustQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.RefundTransferred;
    protected override string? DeadLetterQueueName => QueueName + "_dead_letter";
    protected override async Task HandleMessageAsync(RabbitMessage<RefundTransferredEvent> message, CancellationToken ct)
    {
        if (message.Data == null || message.Data.RefundId == Guid.Empty || message.Data.OrderId == Guid.Empty)
            throw new ArgumentException("Refund and order IDs are required.");
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<RefundEventEffects>().ApplyTrustAsync(message.Data.RefundId, ct);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !ct.IsCancellationRequested)
            { logger.LogWarning(ex, "Refund {Id} retry {Attempt}.", message.Data.RefundId, attempt + 1); await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct); }
        }
    }
}
