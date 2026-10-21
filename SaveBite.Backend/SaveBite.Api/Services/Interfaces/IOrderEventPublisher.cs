using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Interfaces;

// Persist and dispatch durable order lifecycle events.
public interface IOrderEventPublisher
{
    Task QueuePendingAsync(Order order, CancellationToken ct = default);
    Task QueueNoShowAsync(Order order, CancellationToken ct = default);
}
