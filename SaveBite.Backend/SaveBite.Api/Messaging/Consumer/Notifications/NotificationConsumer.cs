using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Messaging.Consumer.Notifications;

public class NotificationConsumer : RabbitMqConsumer<NotificationRequestedEvent>
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationConsumer> _logger;
    public NotificationConsumer(IOptions<RabbitMqOptions> options, ILogger<NotificationConsumer> logger, IServiceScopeFactory scopes)
        : base(options, logger) { _scopes = scopes; _logger = logger; }
    protected override string QueueName => RabbitMqQueues.NotificationQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.NotificationRequested;
    protected override string? DeadLetterQueueName => RabbitMqQueues.NotificationDeadLetterQueueName;

    // Retry in a fresh scope; exhausted failures go to the dead-letter queue for inspection and replay.
    protected override async Task HandleMessageAsync(RabbitMessage<NotificationRequestedEvent> message, CancellationToken ct)
    {
        var data = message.Data;
        if (data == null || data.RequestId == Guid.Empty || data.Target == null || data.Command == null)
            throw new ArgumentException("A notification request requires an identity, target and command.");
        var command = data.Command with { DeduplicationKey = string.IsNullOrWhiteSpace(data.Command.DeduplicationKey)
            ? $"notification-request:{data.RequestId}" : data.Command.DeduplicationKey.Trim() };
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<INotificationService>().SendAsync(data.Target, command, ct);
                return;
            }
            catch (Exception ex) when (attempt < 3 && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Retrying notification request {RequestId}, attempt {Attempt}.", data.RequestId, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), ct);
            }
        }
    }
}
