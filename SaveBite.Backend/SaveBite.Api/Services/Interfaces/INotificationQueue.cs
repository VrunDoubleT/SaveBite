using SaveBite.Backend.Models.DTOs;
namespace SaveBite.Backend.Services.Interfaces;

public interface INotificationQueue
{
    // Publish directly to RabbitMQ after business success; failures are propagated without local persistence.
    Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default);
}
