using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Trust;

public sealed class TrustLevelReconcileConsumer : RabbitMqConsumer<TrustLevelReconcileEvent>
{
    private readonly ILogger<TrustLevelReconcileConsumer> _logger;
    private readonly IServiceScopeFactory _scopes;

    public TrustLevelReconcileConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<TrustLevelReconcileConsumer> logger,
        IServiceScopeFactory scopes) : base(options, logger)
    {
        _logger = logger;
        _scopes = scopes;
    }

    protected override string QueueName => RabbitMqQueues.TrustLevelReconcileQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.TrustLevelReconcileDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.TrustLevelReconcile;

    protected override async Task HandleMessageAsync(
        RabbitMessage<TrustLevelReconcileEvent> message,
        CancellationToken cancellationToken)
    {
        if (message.Data == null || message.Data.PolicyVersion <= 0)
            throw new ArgumentException("A valid policy version is required.");

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ITrustReconciliationService>()
                    .ReconcileAsync(message.Data.PolicyVersion, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Trust reconcile retry {Attempt} for Policy Version {Version}.",
                    attempt + 1, message.Data.PolicyVersion);
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
            }
        }
    }
}