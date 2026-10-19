namespace SaveBite.Backend.Services.Interfaces;

// Handle pending-order scheduling and notify eligible order subscribers.
public interface IShopOrderEventService
{
    Task HandlePendingAsync(Guid orderId, Guid shopId, CancellationToken ct = default);
    Task NotifyAsync(Guid shopId, Guid orderId, string reason, CancellationToken ct = default);
}
