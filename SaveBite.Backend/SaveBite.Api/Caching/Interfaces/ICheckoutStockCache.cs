namespace SaveBite.Backend.Caching.Interfaces;

// Refresh the stock cache after an order changes authoritative inventory.
public interface ICheckoutStockCache
{
    Task SyncAsync(Guid orderId, CancellationToken ct = default);
}
