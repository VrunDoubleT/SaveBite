namespace SaveBite.Backend.Services.Interfaces;

// Reconcile deal status after an order changes PostgreSQL inventory.
public interface IOrderInventorySynchronizer
{
    Task SyncAsync(Guid orderId, CancellationToken ct = default);
}
