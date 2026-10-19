using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

// Persist and lock durable order events until delivery is recorded.
public interface IOrderEventRepository
{
    void Add(OrderEventOutbox message);
    Task SaveAsync(CancellationToken ct);
    Task DispatchAsync(Guid messageId, Func<OrderEventOutbox, Task> publish, CancellationToken ct);
}
