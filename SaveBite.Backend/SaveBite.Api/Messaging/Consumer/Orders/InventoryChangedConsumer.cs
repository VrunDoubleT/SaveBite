using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.Messaging.Consumer.Orders;

public sealed class InventoryChangedConsumer(IOptions<RabbitMqOptions> options, ILogger<InventoryChangedConsumer> logger, IServiceScopeFactory scopes)
    : RabbitMqConsumer<InventoryChangedEvent>(options, logger)
{
    protected override string QueueName => RabbitMqQueues.InventoryQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.InventoryChanged;
    protected override string? DeadLetterQueueName => QueueName + "_dead_letter";
    protected override async Task HandleMessageAsync(RabbitMessage<InventoryChangedEvent> message, CancellationToken ct)
    {
        if (message.Data?.FlashDealIds == null || message.Data.FlashDealIds.Length == 0 || message.Data.FlashDealIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("Deal IDs are required.");
        foreach (var id in message.Data.FlashDealIds.Distinct())
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<DealInventoryService>().ReconcileAsync(id, ct);
                    break;
                }
                catch (Exception ex) when (attempt < 3 && !ct.IsCancellationRequested)
                { logger.LogWarning(ex, "Deal {Id} inventory retry {Attempt}.", id, attempt + 1); await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct); }
            }
        }
    }
}
