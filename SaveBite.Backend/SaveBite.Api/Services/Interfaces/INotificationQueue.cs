using SaveBite.Backend.Models.DTOs;
namespace SaveBite.Backend.Services.Interfaces;

public interface INotificationQueue
{
    // Persist a durable notification request; joins the current database transaction when present.
    Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default);
}
