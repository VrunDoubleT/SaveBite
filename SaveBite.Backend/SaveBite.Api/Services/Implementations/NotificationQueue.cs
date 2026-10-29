using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Publish notification requests directly; only the consumer persists notifications and sends realtime events.
public class NotificationQueue : INotificationQueue
{
    private readonly IRabbitMqPublisher _publisher;

    public NotificationQueue(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    // Await broker confirmation and propagate publish failures so each caller can log or handle them.
    public Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        command = command with
        {
            DeduplicationKey = string.IsNullOrWhiteSpace(command.DeduplicationKey)
                ? $"notification-request:{id}"
                : command.DeduplicationKey.Trim()
        };

        return _publisher.PublishAsync(
            RabbitMqRoutingKeys.NotificationRequested,
            new NotificationRequestedEvent(id, target, command),
            ct);
    }
}
