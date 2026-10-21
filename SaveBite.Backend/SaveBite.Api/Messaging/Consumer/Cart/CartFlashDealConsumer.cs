using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Cart;

// One cart queue listens to all deal-unavailable events independently of other feature consumers.
public class CartFlashDealConsumer : RabbitMqConsumer<FlashDealEvent>
{
    private readonly IServiceScopeFactory _scopes;

    public CartFlashDealConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<CartFlashDealConsumer> logger,
        IServiceScopeFactory scopes)
        : base(options, logger)
    {
        _scopes = scopes;
    }

    protected override string QueueName => RabbitMqQueues.CartFlashDealQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.FlashDealClosed;
    
    protected override IReadOnlyCollection<string> RoutingKeys =>
        [RabbitMqRoutingKeys.FlashDealClosed, RabbitMqRoutingKeys.FlashDealHidden, RabbitMqRoutingKeys.FlashDealExpired];

    // Remove affected cart items through a scoped service when an external deal lifecycle event arrives.
    protected override async Task HandleMessageAsync(RabbitMessage<FlashDealEvent> message, CancellationToken cancellationToken)
    {
        if (message.Data == null || message.Data.FlashDealId == Guid.Empty)
            throw new ArgumentException("The flash deal event must contain Data.FlashDealId.");
        await using var scope = _scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ICartDealEventService>()
            .HandleAsync(message.Data.FlashDealId, cancellationToken);
    }
}
