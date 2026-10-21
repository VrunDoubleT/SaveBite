using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

// Persist and lock outbox messages for retryable broker delivery.
public sealed class OrderEventRepository : IOrderEventRepository
{
    private readonly AppDbContext _db;

    public OrderEventRepository(AppDbContext db) { _db = db; }

    // Track an outbox message in the same database transaction as the order change.
    public void Add(OrderEventOutbox message) => _db.Set<OrderEventOutbox>().Add(message);
    // Persist outbox delivery progress so failed messages can be retried.
    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    // Dispatch jobs serialize on the outbox row and only publish data from a committed checkout transaction.
    public async Task DispatchAsync(Guid messageId, Func<OrderEventOutbox, Task> publish, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var message = await _db.Set<OrderEventOutbox>()
            .FromSqlInterpolated($"SELECT * FROM order_event_outbox WHERE id = {messageId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (message == null) throw new InvalidOperationException("The pending order event has not committed or was rolled back.");
        if (!message.PublishedAt.HasValue)
        {
            await publish(message);
            await _db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
